using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using HorseBot.Configuration;
using Microsoft.Extensions.Options;
using System.Globalization;
using System;

namespace HorseBot.Services
{
    public class GoogleSheetsService
    {
        private readonly SheetsService _sheetsService;
        private readonly IOptions<GoogleSheetsConfiguration> _googleSheetsConfig;
        private readonly string _spreadsheetId = "";

        private const string StudentsSheet = "Students";
        private const string AttendanceSheet = "Attendance";
        private const string PaymentsSheet = "Payments";

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

        public async Task AddStudentAsync(string name)
        {
            var range = $"{StudentsSheet}!A:B";
            var values = new List<IList<object>> { new List<object> { name, 0 } };
            var body = new ValueRange { Values = values };

            try
            {
                var request = _sheetsService.Spreadsheets.Values.Append(body, _spreadsheetId, range);
                request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                await request.ExecuteAsync();
            }
            catch (Exception ex)
            {

            }
        }

        public async Task AddPaymentAsync(string name, int count)
        {
            var getRequest = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, $"{StudentsSheet}!A:B");
            var response = await getRequest.ExecuteAsync();
            var values = response.Values;

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i][0].ToString().ToLower() == name.ToLower())
                {
                    int oldVal = int.Parse(values[i][1].ToString());
                    int newVal = oldVal + count;
                    var updateRange = $"{StudentsSheet}!B{i + 1}";
                    var body = new ValueRange { Values = new List<IList<object>> { new List<object> { newVal } } };

                    var request = _sheetsService.Spreadsheets.Values.Update(body, _spreadsheetId, updateRange);
                    request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                    await request.ExecuteAsync();

                    var date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var payBody = new ValueRange { Values = new List<IList<object>> { new List<object> { name, date, count } } };

                    var requestAppendPayment = _sheetsService.Spreadsheets.Values.Append(payBody, _spreadsheetId, $"{PaymentsSheet}!A:B");
                    requestAppendPayment.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                    await requestAppendPayment.ExecuteAsync();

                    break;
                }
            }
        }

        public async Task<bool> RegisterAttendanceAsync(string name)
        {
            var getRequest = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, $"{StudentsSheet}!A:B");
            var response = await getRequest.ExecuteAsync();
            var values = response.Values;

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i][0].ToString().ToLower() == name.ToLower())
                {
                    int current = int.Parse(values[i][1].ToString());
                    if (current <= 0) return false;

                    int updated = current - 1;
                    var updateRange = $"{StudentsSheet}!B{i + 1}";
                    var updateBody = new ValueRange { Values = new List<IList<object>> { new List<object> { updated } } };
                    
                    var request = _sheetsService.Spreadsheets.Values.Update(updateBody, _spreadsheetId, updateRange);
                    request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                    await request.ExecuteAsync();

                    var date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var attBody = new ValueRange { Values = new List<IList<object>> { new List<object> { name, date } } };

                    var requestAppendAttendance = _sheetsService.Spreadsheets.Values.Append(attBody, _spreadsheetId, $"{AttendanceSheet}!A:B");
                    requestAppendAttendance.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW;
                    await requestAppendAttendance.ExecuteAsync();

                    return true;
                }
            }

            return false;
        }

        public async Task<string> GetStatusAsync(string name)
        {
            var req = await _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, $"{StudentsSheet}!A:B").ExecuteAsync();
            var values = req.Values;

            foreach (var row in values)
            {
                if (row[0].ToString().ToLower() == name.ToLower())
                {
                    return $"У {name} осталось занятий: {row[1]}";
                }
            }

            return $"Ученик {name} не найден.";
        }

        public async Task<string> GetAllStudentsAsync()
        {
            var req = await _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, $"{StudentsSheet}!A:B").ExecuteAsync();
            var list = req.Values;
            var result = "Список учеников:\n";
            foreach (var row in list)
            {
                result += $"{row[0]}: {row[1]} занятий\n";
            }
            return result;
        }

        public async Task<List<string>> GetAllStudentNamesAsync()
        {
            var response = await _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, "Students!A:A").ExecuteAsync();
            return response.Values?
                .Skip(1)
                .Where(r => r.Count > 0)
                .Select(r => r[0].ToString())
                .ToList() ?? new List<string>();
        }


        #region Old methods
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
        #endregion
    }
}
