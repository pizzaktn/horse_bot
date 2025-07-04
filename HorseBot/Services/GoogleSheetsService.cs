using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using HorseBot.Configuration;
using Microsoft.Extensions.Options;

namespace HorseBot.Services
{
    public class GoogleSheetsService
    {
        private readonly SheetsService _sheetsService;
        private readonly IOptions<GoogleSheetsConfiguration> _googleSheetsConfig;
        private readonly string _spreadsheetId = "";

        public GoogleSheetsService(IOptions<GoogleSheetsConfiguration> googleSheetsConfig)
        {
            _googleSheetsConfig = googleSheetsConfig;
            var credential = GoogleCredential.FromFile("horsebot-461419-1c85be030892.json").CreateScoped(SheetsService.Scope.Spreadsheets);
            //var credential = GoogleCredential.FromFile("credentials.json").CreateScoped(SheetsService.Scope.Spreadsheets);
            _sheetsService = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "HorseBot"
            });

            _spreadsheetId = _googleSheetsConfig.Value.SpreadsheetId;
        }

        public async Task AddStudentAsync(string name, long telegramId)
        {
            var range = "Students!A:E";
            var row = new List<object> { "", name, telegramId.ToString(), 0, 0 };
            var valueRange = new ValueRange { Values = new List<IList<object>> { row } };
            var request = _sheetsService.Spreadsheets.Values.Append(valueRange, _spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
            await request.ExecuteAsync();
        }

        public async Task<int> GetBalanceAsync(long telegramId)
        {
            var range = "Students!A:E";
            var response = await _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range).ExecuteAsync();
            var row = response.Values.FirstOrDefault(r => r.Count > 2 && r[2].ToString() == telegramId.ToString());
            return row != null && row.Count > 4 ? int.Parse(row[4].ToString()) : 0;
        }

        public async Task AddPaymentAsync(long telegramId, int amount, int lessons)
        {
            var range = "Students!A:E";
            var response = await _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, range).ExecuteAsync();

            for (int i = 0; i < response.Values.Count; i++)
            {
                var row = response.Values[i];
                if (row.Count > 2 && row[2].ToString() == telegramId.ToString())
                {
                    int currentLessons = int.Parse(row[4].ToString());
                    row[4] = (currentLessons + lessons).ToString();

                    //var valueRange = new ValueRange { Values = new List<IList<object>> { row } };
                    //var updateRange = $"Students!A{i + 1}:E{i + 1}";
                    //await _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, updateRange)
                    //    .SetValueInputOption(SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW)
                    //    .ExecuteAsync();
                    break;
                }
            }
        }

        public async Task<string> InitializeSpreadsheetAsync(string title = "HorseBot")
        {
            var spreadsheet = new Spreadsheet
            {
                Properties = new SpreadsheetProperties { Title = title },
                Sheets = new List<Sheet>
        {
            new Sheet { Properties = new SheetProperties { Title = "Students" } },
            new Sheet { Properties = new SheetProperties { Title = "Payments" } },
            new Sheet { Properties = new SheetProperties { Title = "Schedule" } },
            new Sheet { Properties = new SheetProperties { Title = "Dashboard" } }
        }
            };

            var createRequest = _sheetsService.Spreadsheets.Create(spreadsheet);
            var created = await createRequest.ExecuteAsync();

            var spreadsheetId = created.SpreadsheetId;

            // Заполняем заголовки
            await _sheetsService.Spreadsheets.Values.BatchUpdate(new BatchUpdateValuesRequest
            {
                Data = new List<ValueRange>
        {
            new ValueRange
            {
                Range = "Students!A1:D1",
                Values = new List<IList<object>> { new List<object> { "ChatId", "Имя", "Всего занятий", "Осталось" } }
            },
            new ValueRange
            {
                Range = "Payments!A1:D1",
                Values = new List<IList<object>> { new List<object> { "ChatId", "Дата", "Сумма", "Кол-во занятий" } }
            },
            new ValueRange
            {
                Range = "Schedule!A1:D1",
                Values = new List<IList<object>> { new List<object> { "ChatId", "День недели", "Время", "Комментарий" } }
            },
            new ValueRange
            {
                Range = "Dashboard!A1:B1",
                Values = new List<IList<object>> { new List<object> { "Метрика", "Значение" } }
            }
        },
                ValueInputOption = "RAW"
            }, spreadsheetId).ExecuteAsync();

            Console.WriteLine("Создана Google Таблица: " + created.SpreadsheetUrl);
            return spreadsheetId;
        }
    }
}
