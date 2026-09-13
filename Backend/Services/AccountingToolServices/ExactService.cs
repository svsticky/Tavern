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

        private string Division => _db.Settings.FirstOrDefault(s => s.Name == "ExactDivision")?.Value
            ?? Environment.GetEnvironmentVariable("EXACT_DIVISION")
            ?? "";

        /// <summary>
        /// Cost centers/units are optional; treat a blank value as "not set" so it is omitted from the Exact payload
        /// instead of being sent as an empty string.
        /// </summary>
        private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

        private string AccessToken => _db.Settings.FirstOrDefault(s => s.Name == "ExactAccessToken")?.Value
            ?? Environment.GetEnvironmentVariable("EXACT_ACCESS_TOKEN")
            ?? "";

        private string PaymentService => _db.Settings.FirstOrDefault(s => s.Name == "PaymentProvider")?.Value
            ?? Environment.GetEnvironmentVariable("PAYMENT_PROVIDER")
            ?? "MOLLIE";

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
            var paymentCondition = _db.Settings.FirstOrDefault(s => s.Name == "PaymentServicePaymentsCondition")?.Value;
            var customer = _db.Settings.FirstOrDefault(s => s.Name == "PaymentServiceRelationCode")?.Value;

            return new
            {
                EntryDate = DateTime.UtcNow,
                Description = $"{char.ToUpper(pService[0])}{pService.Substring(1).ToLower()} payment {payment.PaymentServiceId}",
                YourRef = $"{GetYourRefPrefix(payment)}-{payment.Id}",
                PaymentCondition = NullIfBlank(paymentCondition),
                Customer = NullIfBlank(customer),

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
            var activityGLFallback = _db.Settings.FirstOrDefault(s => s.Name == "ActivityGLAccount")?.Value ?? "7001";
            return new
            {
                GLAccount = payment.Activity?.GLAccountId ?? payment.Activity?.Organizer?.DefaultGLAccount ?? activityGLFallback,
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
                GLAccount = _db.Settings.FirstOrDefault(s => s.Name == "MembershipGLAccount")?.Value ?? "8000",
                Description = "Lidmaatschap",
                VATCode = _db.Settings.FirstOrDefault(s => s.Name == "MembershipVATCode")?.Value ?? "0",
                CostCenter = NullIfBlank(_db.Settings.FirstOrDefault(s => s.Name == "MembershipCostCenter")?.Value),
                CostUnit = NullIfBlank(_db.Settings.FirstOrDefault(s => s.Name == "MembershipCostUnit")?.Value),
                AmountDC = payment.Price
            };
        }

        private object BuildPaymentFeeLine(PaymentServiceFeePayment payment)
        {
            var pService = string.IsNullOrEmpty(PaymentService) ? "Mollie" : PaymentService;
            return new
            {
                GLAccount = _db.Settings.FirstOrDefault(s => s.Name == "PaymentServiceFeeGLAccount")?.Value ?? "4900",
                Description = $"{char.ToUpper(pService[0])}{pService.Substring(1).ToLower()} fee",
                VATCode = _db.Settings.FirstOrDefault(s => s.Name == "PaymentServiceFeeVATCode")?.Value ?? "21",
                CostCenter = NullIfBlank(_db.Settings.FirstOrDefault(s => s.Name == "PaymentServiceFeeCostCenter")?.Value),
                CostUnit = NullIfBlank(_db.Settings.FirstOrDefault(s => s.Name == "PaymentServiceFeeCostUnit")?.Value),
                AmountDC = payment.Price
            };
        }

        private object BuildBegunstigerLine(BegunstigerPayment payment)
        {
            return new
            {
                GLAccount = _db.Settings.FirstOrDefault(s => s.Name == "BegunstigerGLAccount")?.Value
                    ?? _db.Settings.FirstOrDefault(s => s.Name == "MembershipGLAccount")?.Value
                    ?? "8000",
                Description = "Begunstiger",
                VATCode = _db.Settings.FirstOrDefault(s => s.Name == "BegunstigerVATCode")?.Value ?? "0",
                CostCenter = NullIfBlank(_db.Settings.FirstOrDefault(s => s.Name == "BegunstigerCostCenter")?.Value),
                CostUnit = NullIfBlank(_db.Settings.FirstOrDefault(s => s.Name == "BegunstigerCostUnit")?.Value),
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
