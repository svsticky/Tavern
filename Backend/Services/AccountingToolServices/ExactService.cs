using Backend.Database;
using Backend.Models.Domain;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.Services.AccountingToolServices
{
    /// <summary>
    /// Implements synchronization of payment data to Exact Online.
    /// </summary>
    public class ExactService(
        HttpClient http,
        PostgresDbContext db,
        ILogger<ExactService> logger) : AbstractAccountingToolService(db, logger)
    {
        private static readonly JsonSerializerOptions _salesEntryJsonOptions = new()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _http = http;

        private string Division => _db.Settings.Find("ExactDivision")?.Value ?? "";

        /// <summary>
        /// Cost centers/units are optional; treat a blank value as "not set" so it is omitted from the Exact payload
        /// instead of being sent as an empty string.
        /// </summary>
        private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

        private string AccessToken => _db.Settings.Find("ExactAccessToken")?.Value ?? "";

        private string PaymentService => _db.Settings.Find("PaymentProvider")?.Value ?? "MOLLIE";

        /// <inheritdoc />
        protected override async Task<Guid> SyncPaymentCoreAsync(Payment payment, CancellationToken ct)
        {
            if (payment == null)
                throw new ArgumentNullException(nameof(payment));
            _logger.LogInformation("Syncing payment {PaymentId} to Exact. Type: {PaymentType}", payment.Id, payment.GetType().Name);

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", AccessToken);

            var existingId = await FindExistingSalesEntryId(payment, ct);

            if (existingId != null)
            {
                _logger.LogInformation("Payment {PaymentId} already synced to Exact with entry {EntryId}.", payment.Id, existingId.Value);
                return existingId.Value;
            }

            var salesEntry = BuildSalesEntry(payment);

            var json = JsonSerializer.Serialize(salesEntry, _salesEntryJsonOptions);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(
                $"{Division}/salesentry/SalesEntries",
                content,
                ct
            );

            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Exact sync failed for payment {PaymentId}. Status: {StatusCode}", payment.Id, response.StatusCode);
                throw new Exception($"Exact sync failed: {responseBody}");
            }

            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var createdId = responseJson.GetProperty("ID").GetGuid();
            _logger.LogInformation("Payment {PaymentId} synced to Exact with entry {EntryId}.", payment.Id, createdId);
            return createdId;
        }

        private object BuildSalesEntry(Payment payment)
        {
            var pService = string.IsNullOrEmpty(PaymentService) ? "Mollie" : PaymentService;
            return new
            {
                EntryDate = DateTime.UtcNow,
                Description = $"{char.ToUpper(pService[0])}{pService.Substring(1).ToLower()} payment {payment.PaymentServiceId}",
                YourRef = $"{GetYourRefPrefix(payment)}-{payment.Id}",

                SalesEntryLines = new[]
                {
                    BuildLine(payment)
                }
            };
        }

        /// <summary>
        /// Labels the sales entry's YourRef by payment type, so entries for different payment types
        /// (and the lookup in FindExistingSalesEntryId) don't get ambiguously grouped together.
        /// </summary>
        private static string GetYourRefPrefix(Payment payment)
        {
            return payment switch
            {
                EnrollmentPayment => "Enrollment payment",
                MembershipPayment => "Membership payment",
                PaymentServiceFeePayment => "Payment service fee payment",
                BegunstigerPayment => "Begunstiger payment",
                _ => throw new Exception("Unsupported payment type")
            };
        }

        private object BuildLine(Payment payment)
        {
            return payment switch
            {
                EnrollmentPayment ep => BuildEnrollmentLine(ep),
                MembershipPayment mp => BuildMembershipLine(mp),
                PaymentServiceFeePayment mfp => BuildPaymentFeeLine(mfp),
                BegunstigerPayment bp => BuildBegunstigerLine(bp),
                _ => throw new Exception("Unsupported payment type")
            };
        }

        private object BuildEnrollmentLine(EnrollmentPayment payment)
        {
            return new
            {
                GLAccount = payment.Activity?.GLAccountId ?? payment.Activity?.Organizer?.DefaultGLAccount,
                Description = $"{payment.Activity?.Organizer?.Name ?? ""} | {payment.Activity?.Name}",
                VATCode = MapVat(payment.Activity?.VatRate),
                CostCenter = NullIfBlank(payment.Activity?.CostCenterId ?? payment.Activity?.Organizer?.DefaultCostCenter),
                CostUnit = NullIfBlank(payment.Activity?.CostUnitId),
                AmountDC = payment.Price
            };
        }

        private object BuildMembershipLine(MembershipPayment payment)
        {
            return new
            {
                GLAccount = _db.Settings.Where(s => s.Name == "MembershipGLAccount").Select(s => s.Value).FirstOrDefault(),
                Description = "Lidmaatschap",
                VATCode = _db.Settings.Where(s => s.Name == "MembershipVATCode").Select(s => s.Value).FirstOrDefault() ?? "0",
                CostCenter = NullIfBlank(_db.Settings.Where(s => s.Name == "MembershipCostCenter").Select(s => s.Value).FirstOrDefault()),
                CostUnit = NullIfBlank(_db.Settings.Where(s => s.Name == "MembershipCostUnit").Select(s => s.Value).FirstOrDefault()),
                AmountDC = payment.Price
            };
        }

        private object BuildPaymentFeeLine(PaymentServiceFeePayment payment)
        {
            var pService = string.IsNullOrEmpty(PaymentService) ? "Mollie" : PaymentService;
            return new
            {
                GLAccount = _db.Settings.Where(s => s.Name == "PaymentServiceFeeGLAccount").Select(s => s.Value).FirstOrDefault(),
                Description = $"{char.ToUpper(pService[0])}{pService.Substring(1).ToLower()} fee",
                VATCode = _db.Settings.Where(s => s.Name == "PaymentServiceFeeVATCode").Select(s => s.Value).FirstOrDefault() ?? "21",
                CostCenter = NullIfBlank(_db.Settings.Where(s => s.Name == "PaymentServiceFeeCostCenter").Select(s => s.Value).FirstOrDefault()),
                CostUnit = NullIfBlank(_db.Settings.Where(s => s.Name == "PaymentServiceFeeCostUnit").Select(s => s.Value).FirstOrDefault()),
                AmountDC = payment.Price
            };
        }

        private object BuildBegunstigerLine(BegunstigerPayment payment)
        {
            return new
            {
                GLAccount = _db.Settings.Where(s => s.Name == "BegunstigerGLAccount").Select(s => s.Value).FirstOrDefault(),
                Description = "Begunstiger",
                VATCode = _db.Settings.Where(s => s.Name == "BegunstigerVATCode").Select(s => s.Value).FirstOrDefault() ?? "0",
                CostCenter = NullIfBlank(_db.Settings.Where(s => s.Name == "BegunstigerCostCenter").Select(s => s.Value).FirstOrDefault()),
                CostUnit = NullIfBlank(_db.Settings.Where(s => s.Name == "BegunstigerCostUnit").Select(s => s.Value).FirstOrDefault()),
                AmountDC = payment.Price
            };
        }

        private string MapVat(uint? vatRate)
        {
            return vatRate switch
            {
                0 => "0",
                9 => "L",
                21 => "H",
                _ => "H"
            };
        }

        private async Task<Guid?> FindExistingSalesEntryId(Payment payment, CancellationToken ct)
        {
            var division = Division;

            var yourRef = $"{GetYourRefPrefix(payment)}-{payment.Id}";

            var url = $"{division}/salesentry/SalesEntries?$filter=YourRef eq '{yourRef}'";

            var response = await _http.GetAsync(url, ct);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);

            var results = doc.RootElement.GetProperty("d").GetProperty("results");

            if (results.GetArrayLength() == 0)
                return null;

            return results[0].GetProperty("ID").GetGuid();
        }
    }
}
