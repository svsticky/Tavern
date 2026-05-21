using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Backend.Services
{
    /// <summary>
    /// Implements synchronization of payment data to Exact Online.
    /// </summary>
    public class ExactService : IAccountingToolService
    {
        private readonly HttpClient _http;
        private readonly PostgresDbContext _db;
        private readonly ILogger<ExactService> _logger;

        private readonly string _division = Environment.GetEnvironmentVariable("EXACT_DIVISION")!;

        private readonly string _accessToken = Environment.GetEnvironmentVariable("EXACT_ACCESS_TOKEN")!;

        private readonly string _membershipGLAccount = Environment.GetEnvironmentVariable("EXACT_MEMBERSHIP_GL_ACCOUNT")!;

        /// <summary>
        /// Initializes a new instance of the ExactService class with the specified HTTP client, database context, and logger. The constructor sets up the necessary dependencies for the service to function correctly, allowing it to make HTTP requests to the Exact Online API, interact with the database to retrieve payment information, and log important events and errors that occur during the synchronization process. This setup is essential for ensuring that the service can effectively synchronize payment data to Exact Online while providing visibility into its operations through logging.
        /// </summary>
        /// <param name="http">The HTTP client.</param>
        /// <param name="db">The database context.</param>
        /// <param name="logger">The logger.</param>
        public ExactService(HttpClient http, PostgresDbContext db, ILogger<ExactService> logger)
        {
            _http = http;
            _db = db;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<Guid> SyncPaymentAsync(Payment payment, CancellationToken ct)
        {
            if (payment == null)
                throw new ArgumentNullException(nameof(payment));
            _logger.LogInformation("Syncing payment {PaymentId} to Exact. Type: {PaymentType}", payment.Id, payment.GetType().Name);

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);

            var existingId = await FindExistingSalesEntryId(payment, ct);

            if (existingId != null)
            {
                _logger.LogInformation("Payment {PaymentId} already synced to Exact with entry {EntryId}.", payment.Id, existingId.Value);
                return existingId.Value;
            }

            var salesEntry = BuildSalesEntry(payment);

            var json = JsonSerializer.Serialize(salesEntry);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync(
                $"{_division}/salesentry/SalesEntries",
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
            return new
            {
                EntryDate = DateTime.UtcNow,
                Description = $"Mollie payment {payment.MollieId}",
                YourRef = $"{(payment is EnrollmentPayment ? "Enrollment payment" : "Membership payment")}-{payment.Id}",

                SalesEntryLines = new[]
                {
                    BuildLine(payment)
                }
            };
        }

        private object BuildLine(Payment payment)
        {
            return payment switch
            {
                EnrollmentPayment ep => BuildEnrollmentLine(ep),
                MembershipPayment mp => BuildMembershipLine(mp),
                MollieFeePayment mfp => BuildMollieFeeLine(mfp),
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
                CostCenter = payment.Activity?.CostCenterId ?? payment.Activity?.Organizer?.DefaultCostCenter,
                CostUnit = payment.Activity?.CostUnitId,
                AmountDC = payment.Price
            };
        }

        private object BuildMembershipLine(MembershipPayment payment)
        {
            return new
            {
                GLAccount = _db.Settings.Where(s => s.Name == "MembershipGLAccount").Select(s => s.Value).FirstOrDefault(),
                Description = "Lidmaatschap",
                VATCode = "0",
                AmountDC = payment.Price
            };
        }

        private object BuildMollieFeeLine(MollieFeePayment payment)
        {
            return new
            {
                GLAccount = _db.Settings.Where(s => s.Name == "MollieFeeGLAccount").Select(s => s.Value).FirstOrDefault(),
                Description = "Mollie fee",
                VATCode = "0",
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
            var division = _division;

            var yourRef = $"{(payment is EnrollmentPayment ? "Enrollment payment" : "Membership payment")}-{payment.Id}";

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
