using System;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Telegram.Bot.Types.InputFiles;

namespace MyFirstBot
{
    class Program
    {
        private static string Token { get; } = System.IO.File.ReadAllText(@"C:\source\token.txt");

        private static TelegramBotClient myBot;

        private static readonly string link = "https://iz.ru";

        static void Main(string[] args)
        {
            myBot = new TelegramBotClient(Token);
            Thread thread = new Thread(StartBot);
            thread.Start();
            Console.ReadLine();
        }
        /// <summary>
        /// Стартовый метод запуск бота
        /// </summary>
        static void StartBot()
        {
            using var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };

            myBot.StartReceiving(
                UpdateAsync,
                ErrorMessage,
                receiverOptions,
                cancellationToken: cts.Token);

            var res = GetData(myBot);

            Console.WriteLine($"Получение сообщений от @{res.Result.Username}");

            Console.ReadLine();

            Thread.Sleep(100);

            cts.Cancel(); //Отпрака отмены для остановки бота
        }
        /// <summary>
        /// Обработка данных принятых ботом
        /// </summary>
        /// <param name="botClient"></param>
        /// <param name="update"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        async static Task UpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type != UpdateType.Message)
                return;

            var chatId = update.Message.Chat.Id;
            var messageText = update.Message.Text;
            var name = update.Message.From.FirstName;
            var type = update.Message.Type;
            switch (update.Message.Type)
            {
                case MessageType.Text:
                    {
                        if (messageText != null)
                        {
                            switch (messageText)
                            {
                                case "/start":
                                    {
                                        await botClient.SendTextMessageAsync(
                                            chatId: chatId,
                                            text: "\n Вас приветствует бот Andy" +
                                                  "\n выберите действие с помощью кнопок",
                                            cancellationToken: cancellationToken,
                                            replyMarkup: GetButtonsText());
                                        break;
                                    }
                                case "Новости":
                                case "/news":
                                    {
                                        List<string> listSample = GetNews(link);
                                        List<string> listLinks = GetLinkNews(listSample, link);
                                        List<string> listText = GetTextLinks(listSample);
                                        SendNewsAsync(botClient, update, listLinks, listText);
                                        break;
                                    }
                                case "Курс валют":
                                case "/exchangeRates":
                                    {
                                        await botClient.SendTextMessageAsync(
                                                chatId: chatId,
                                                text: "Курс валют",
                                                cancellationToken: cancellationToken,
                                                replyMarkup: GetButtonsExchange());
                                        break;
                                    }
                                case "Основное меню":
                                    {
                                        await botClient.SendTextMessageAsync(
                                                chatId: chatId,
                                                text: "Основное меню",
                                                cancellationToken: cancellationToken,
                                                replyMarkup: GetButtonsText());
                                        break;
                                    }
                                case "USD":
                                case "EUR":
                                case "GBP":
                                case "CHF":
                                    {
                                        string valueCurrency = GetExchange(messageText);

                                        await botClient.SendTextMessageAsync(
                                            chatId: chatId,
                                            text: messageText + " - " + valueCurrency + " RUB",
                                            cancellationToken: cancellationToken,
                                            replyMarkup: GetButtonsExchange());
                                        break;
                                    }
                                case "Список команд":
                                case "/help":
                                    {
                                        await botClient.SendTextMessageAsync(
                                            chatId: chatId,
                                            text: "\n /start - стартовое меню" +
                                                  "\n /news - новости" +
                                                  "\n /exchangeRates - курс валют" +
                                                  "\n /help - список команд",
                                            cancellationToken: cancellationToken,
                                            replyMarkup: GetButtonsText());
                                        break;
                                    }
                                case "Загруженные файлы":
                                    {
                                        GetDownloadFiles(chatId, botClient);
                                        break;
                                    }
                                default:
                                    {
                                        await botClient.SendTextMessageAsync(
                                             chatId: chatId,
                                             text: "\n Выберите действие с помощью кнопок или команду",
                                             cancellationToken: cancellationToken,
                                             replyMarkup: GetButtonsText());
                                        break;
                                    }
                            }
                        }
                        Console.WriteLine($"Принято сообщение: '{messageText}' из чата с номером: {chatId} от {name}.");
                        Thread.Sleep(1000);
                        break;
                    }
                case MessageType.Audio:
                    {
                        DownLoadAsync(update.Message.Audio.FileId, update.Message.Audio.FileName, botClient);
                        break;
                    }
                case MessageType.Document:
                    {
                        DownLoadAsync(update.Message.Document.FileId, update.Message.Document.FileName, botClient);
                        break;
                    }
                case MessageType.Photo:
                    {
                        var msg = GetJToken();
                        foreach (var m in msg)
                        {
                            string fileId = m["message"]["photo"][0]["file_id"].ToString();
                            string fileName = fileId.Substring(0, 7);
                            DownLoadAsync(fileId, fileName, botClient);
                        }
                        break;
                    }
                case MessageType.Sticker:
                    {
                        DownLoadAsync(update.Message.Sticker.FileId, update.Message.Sticker.FileId, botClient);
                        break;
                    }
                case MessageType.Video:
                    {
                        DownLoadAsync(update.Message.Video.FileId, update.Message.Video.FileId.Substring(0, 7) + ".mp4", botClient);
                        break;
                    }
                case MessageType.Unknown:
                    {
                        await botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Вы отправили неизвестный тип",
                        cancellationToken: cancellationToken,
                        replyMarkup: GetButtonsText());
                        break;
                    }
            }
            // Ответ бота
            await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "Вы отправили:\n" + "Тип сообщения:" + type,
            cancellationToken: cancellationToken);
        }
        /// <summary>
        /// Получение данных с сайта новостей
        /// </summary>
        /// <param name="link"></param>
        /// <returns></returns>
        private static List<string> GetNews(string link)
        {
            using (WebClient wc = new WebClient())
            {
                string html = wc.DownloadString(link);

                string expr = "\\/\\d{7}\\/\\d{4}\\-\\d{2}\\-\\d{2}\\/.+?(?=<\\/span\\>)";

                Regex reg = new Regex(expr);

                MatchCollection mc = reg.Matches(html);

                List<string> sample = new List<string>();

                if (mc.Count > 0)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        sample.Add(mc[i].Value);
                    }
                }
                return sample;
            }
        }
        /// <summary>
        /// Получение ссылок новостей
        /// </summary>
        /// <param name="list"></param>
        /// <param name="link"></param>
        /// <returns></returns>
        private static List<string> GetLinkNews(List<string> list, string link)
        {
            List<string> listLinks = new List<string>();

            string pattern = "\"|\'|\\s.+";

            for (int i = 0; i < list.Count; i++)
            {
                listLinks.Add(Regex.Replace(list[i], pattern, String.Empty));
                listLinks[i] = link + listLinks[i];
            }

            return listLinks;
        }
        /// <summary>
        /// Получение заголовков нвостей
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private static List<string> GetTextLinks(List<string> list)
        {
            List<string> listText = new List<string>();

            string pattern = "\\/\\d{7}\\/\\d{4}\\-\\d{2}\\-\\d{2}\\/.+?(?=[а-я]|[А-Я])";

            for (int i = 0; i < list.Count; i++)
            {
                listText.Add(Regex.Replace(list[i], pattern, String.Empty));
            }

            return listText;
        }
        /// <summary>
        /// Отправка новостей боту
        /// </summary>
        /// <param name="bot"></param>
        /// <param name="update"></param>
        /// <param name="listLinks"></param>
        /// <param name="listText"></param>
        private async static void SendNewsAsync(ITelegramBotClient bot, Update update, List<string> listLinks, List<string> listText)
        {
            InlineKeyboardMarkup inlineKeyboard;

            for (int i = 0; i < listLinks.Count; i++)
            {
                inlineKeyboard = InlineKeyboardButton.WithUrl(text: listText[i], url: listLinks[i]);

                await bot.SendTextMessageAsync(update.Message.Chat, $"Новость - {i + 1}", replyMarkup: inlineKeyboard);
            }

        }
        /// <summary>
        /// Получение курса валюты
        /// </summary>
        /// <param name="messageText"></param>
        /// <returns></returns>
        private static string GetExchange(string messageText)
        {
            using (WebClient wc = new WebClient())
            {
                string link = "http://www.finmarket.ru/currency/rates/";

                string html = wc.DownloadString(link);

                char[] letters = messageText.ToCharArray();
                char letterOne = letters[0];
                char letterTwo = letters[1];
                char letterThree = letters[2];

                string expr = String.Format("[{0}][{1}][{2}]\\/.+?,\\d{3}", letterOne, letterTwo, letterThree, "{4}");

                Regex reg = new Regex(expr);

                Match mc = reg.Match(html);

                string text = mc.Value.ToString();
                int lastInd = text.LastIndexOf('>', text.Length - 1);
                string valueCurrency = text.Substring(lastInd + 1, 7);
                return valueCurrency;
            }
        }
        /// <summary>
        /// Кнопки основного меню
        /// </summary>
        /// <returns></returns>
        private static IReplyMarkup GetButtonsText()
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
                           {
                            new KeyboardButton[] { "Новости", "Курс валют" },
                            new KeyboardButton[] { "Список команд" },
                            new KeyboardButton[] { "Загруженные файлы" }
                            })
            {
                ResizeKeyboard = true
            };

            return replyKeyboardMarkup;
        }
        /// <summary>
        /// Кнопки получения курса валют
        /// </summary>
        /// <returns></returns>
        private static IReplyMarkup GetButtonsExchange()
        {
            ReplyKeyboardMarkup replyKeyboardMarkup = new ReplyKeyboardMarkup(new[]
                           {
                            new KeyboardButton[] { "USD", "EUR" },
                            new KeyboardButton[] { "GBP", "CHF" },
                            new KeyboardButton[] { "Основное меню" }
                            })
            {
                ResizeKeyboard = true
            };

            return replyKeyboardMarkup;
        }

        static Task ErrorMessage(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine(exception);
            return Task.CompletedTask;
        }
        /// <summary>
        /// Получение данных о боте
        /// </summary>
        /// <param name="myBotClient"></param>
        /// <returns></returns>
        async static ValueTask<User> GetData(TelegramBotClient myBotClient)
        {
            return await myBotClient.GetMeAsync();
        }
        /// <summary>
        /// Получение JSON объекта
        /// </summary>
        /// <returns></returns>
        static JToken GetJToken()
        {
            WebClient webClient = new WebClient() { Encoding = Encoding.UTF8 };
            int updateId = 0;
            string url = $@"https://api.telegram.org/bot{Token}/getUpdates?offset={updateId}";
            var r = webClient.DownloadString(url);
            return JObject.Parse(r)["result"];
        }
        /// <summary>
        /// Загрузка файлов в папку приложения
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="path"></param>
        /// <param name="botClient"></param>
        async static void DownLoadAsync(string fileId, string path, ITelegramBotClient botClient)
        {
            var file = await botClient.GetFileAsync(fileId);
            FileStream fileStream = new FileStream("_" + path, FileMode.Create);
            await botClient.DownloadFileAsync(file.FilePath, fileStream);
            fileStream.Close();
            fileStream.Dispose();
        }
        /// <summary>
        /// Просмотр отправленных файлов
        /// </summary>
        /// <param name="chatId"></param>
        /// <param name="botClient"></param>
        static async void GetDownloadFiles(long chatId, ITelegramBotClient botClient)
        {
            string currentDir = Environment.CurrentDirectory;
            string[] files = Directory.GetFiles(currentDir, "_*");
            var info = new DirectoryInfo(currentDir);
            FileInfo[] fileInfoList = info.GetFiles("_*");

            if (fileInfoList.Length != 0)
            {
                foreach (var fileInfo in fileInfoList)
                {
                    await using Stream stream = System.IO.File.OpenRead(fileInfo.FullName);
                    Message message = await botClient.SendDocumentAsync(
                        chatId: chatId,
                        document: new InputOnlineFile(content: stream, fileName: fileInfo.Name),
                        caption: fileInfo.Name
                    );

                }
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, $"Файлы еще не загружены", replyMarkup: GetButtonsText());
            }
        }
    }
}
