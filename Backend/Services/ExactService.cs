using Backend.Interfaces;
using Backend.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Backend.Services
{
    public class ExactService : IExactService
    {
        private readonly HttpClient _http;

        private readonly string _division = Environment.GetEnvironmentVariable("EXACT_DIVISION")!;

        private readonly string _accessToken = Environment.GetEnvironmentVariable("EXACT_ACCESS_TOKEN")!;

        private readonly string _membershipGLAccount = Environment.GetEnvironmentVariable("EXACT_MEMBERSHIP_GL_ACCOUNT")!;

        public ExactService(HttpClient http)
        {
            _http = http;
        }

        public async Task<Guid> SyncPaymentAsync(Payment payment, CancellationToken ct)
        {
            if (payment == null)
                throw new ArgumentNullException(nameof(payment));

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessToken);

            var existingId = await FindExistingSalesEntryId(payment, ct);

            if (existingId != null)
                return existingId.Value;

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
                throw new Exception($"Exact sync failed: {responseBody}");
            }

            var responseJson = JsonSerializer.Deserialize<JsonElement>(responseBody);
            return responseJson.GetProperty("ID").GetGuid();
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
                _ => throw new Exception("Unsupported payment type")
            };
        }

        private object BuildEnrollmentLine(EnrollmentPayment payment)
        {
            return new
            {
                GLAccount = payment.Activity.GLAccountId ?? payment.Activity.Organizer?.DefaultGLAccount,
                Description = $"{payment.Activity.Organizer?.Name ?? ""} | {payment.Activity.Name}",
                VATCode = MapVat(payment.Activity.VatRate),
                CostCenter = payment.Activity.CostCenterId ?? payment.Activity.Organizer?.DefaultCostCenter,
                CostUnit = payment.Activity.CostUnitId ?? payment.Activity.Organizer?.DefaultCostUnit,
                AmountDC = payment.Price
            };
        }

        private object BuildMembershipLine(MembershipPayment payment)
        {
            return new
            {
                GLAccount = _membershipGLAccount,
                Description = "Lidmaatschap",
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