using System;
using System.IO;
using System.Linq;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using TourAgency2018.Models;

namespace TourAgency2018.Services
{
    public static class PdfGeneratorService
    {
        private static readonly string FontPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");

        private static Font GetFont(float size, int style = Font.NORMAL)
        {
            var bf = BaseFont.CreateFont(FontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
            return new Font(bf, size, style);
        }

        private static Font TitleFont()  => GetFont(20, Font.BOLD);
        private static Font HeaderFont() => GetFont(14, Font.BOLD);
        private static Font BodyFont()   => GetFont(11);
        private static Font SmallFont()  => GetFont(9);

        private static void AddHeader(Document doc, string title, string subtitle)
        {
            doc.Add(new Paragraph("ТУРИСТИЧЕСКОЕ АГЕНТСТВО 2018", HeaderFont()) { Alignment = Element.ALIGN_CENTER });
            doc.Add(new Paragraph(title, TitleFont()) { Alignment = Element.ALIGN_CENTER, SpacingBefore = 6 });
            doc.Add(new Paragraph(subtitle, SmallFont()) { Alignment = Element.ALIGN_CENTER });
            doc.Add(new Chunk(new LineSeparator()));
            doc.Add(Chunk.NEWLINE);
        }

        private static void AddRow(Document doc, string label, string value)
        {
            var p = new Paragraph { SpacingBefore = 4 };
            p.Add(new Chunk(label + ": ", GetFont(11, Font.BOLD)));
            p.Add(new Chunk(value, BodyFont()));
            doc.Add(p);
        }

        private static void AddNote(Document doc, string text)
        {
            doc.Add(Chunk.NEWLINE);
            doc.Add(new Paragraph(text, SmallFont()) { Alignment = Element.ALIGN_CENTER });
        }

        private static Document CreateDoc(string path, out FileStream fs)
        {
            fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            var doc = new Document(PageSize.A4, 50, 50, 60, 60);
            PdfWriter.GetInstance(doc, fs);
            doc.Open();
            return doc;
        }

        private static string FormatDate(DateTime? date) =>
            date.HasValue ? date.Value.ToString("dd.MM.yyyy") : "не указана";

        private static string FullName(User u) =>
            $"{u.Surname} {u.Name} {u.Patronymic}".Trim();

        // ─── Ваучер на трансфер ───────────────────────────────────────────────

        public static void GenerateTransferVoucher(string path, TourApplication app)
        {
            var tour  = app.Tour;
            var hotel = tour?.Hotels?.FirstOrDefault();

            FileStream fs;
            var doc = CreateDoc(path, out fs);
            using (fs)
            {
                AddHeader(doc, "ВАУЧЕР НА ТРАНСФЕР", $"№ {app.Id} от {FormatDate(app.Date)}");

                AddRow(doc, "Клиент",        FullName(app.User));
                AddRow(doc, "Тур",           tour?.Name ?? "—");
                AddRow(doc, "Дата вылета",   FormatDate(tour?.StartDate));
                AddRow(doc, "Дата возврата", FormatDate(tour?.EndDate));
                AddRow(doc, "Откуда",        "Аэропорт Шереметьево (SVO), Терминал B");
                AddRow(doc, "Куда",          hotel != null ? $"Отель «{hotel.Name}»" : "Отель (по туру)");
                AddRow(doc, "Вид трансфера", "Автобус / минивэн (шаттл)");
                AddRow(doc, "Кол-во мест",   "1");

                doc.Add(Chunk.NEWLINE);
                doc.Add(new Chunk(new LineSeparator()));
                AddNote(doc, "Данный ваучер является подтверждением заказа трансфера. Предъявите его водителю при посадке.");
                AddNote(doc, $"Документ сформирован: {DateTime.Today:dd.MM.yyyy}");

                doc.Close();
            }
        }

        // ─── Ваучер на заселение ─────────────────────────────────────────────

        public static void GenerateHotelVoucher(string path, TourApplication app)
        {
            var tour  = app.Tour;
            var hotel = tour?.Hotels?.FirstOrDefault();

            FileStream fs;
            var doc = CreateDoc(path, out fs);
            using (fs)
            {
                AddHeader(doc, "ВАУЧЕР НА ЗАСЕЛЕНИЕ В ОТЕЛЬ", $"№ {app.Id} от {FormatDate(app.Date)}");

                AddRow(doc, "Клиент",       FullName(app.User));
                AddRow(doc, "Тур",          tour?.Name ?? "—");
                AddRow(doc, "Отель",        hotel?.Name ?? "Уточняется");
                AddRow(doc, "Страна",       hotel?.Country?.Name ?? "Уточняется");
                AddRow(doc, "Звёздность",   hotel != null ? $"{hotel.CountOfStars} ★" : "—");
                AddRow(doc, "Тип питания",  hotel?.MealType?.Name ?? "Уточняется");
                AddRow(doc, "Дата заезда",  FormatDate(tour?.StartDate));
                AddRow(doc, "Дата выезда",  FormatDate(tour?.EndDate));
                AddRow(doc, "Тип номера",   "Стандартный двухместный (DBL)");
                AddRow(doc, "Кол-во ночей", tour?.StartDate.HasValue == true && tour.EndDate.HasValue
                    ? $"{(tour.EndDate.Value - tour.StartDate.Value).Days}"
                    : "уточняется");

                doc.Add(Chunk.NEWLINE);
                doc.Add(new Chunk(new LineSeparator()));
                AddNote(doc, "Предъявите данный ваучер на стойке регистрации отеля при заезде.");
                AddNote(doc, $"Документ сформирован: {DateTime.Today:dd.MM.yyyy}");

                doc.Close();
            }
        }

        // ─── Авиабилет ───────────────────────────────────────────────────────

        public static void GenerateAirTicket(string path, TourApplication app)
        {
            var tour    = app.Tour;
            var hotel   = tour?.Hotels?.FirstOrDefault();
            var country = hotel?.Country?.Name ?? "страна тура";

            FileStream fs;
            var doc = CreateDoc(path, out fs);
            using (fs)
            {
                AddHeader(doc, "АВИАБИЛЕТ", $"Номер бронирования: ТА-{app.Id:D6}");

                AddRow(doc, "Пассажир",        FullName(app.User));
                AddRow(doc, "Маршрут туда",    $"Москва (SVO) → {country}");
                AddRow(doc, "Дата вылета",     FormatDate(tour?.StartDate));
                AddRow(doc, "Рейс",            $"SU-{100 + app.Id % 900}");
                AddRow(doc, "Отправление",     "08:30");
                AddRow(doc, "Прибытие",        "14:45 (местное время)");
                AddRow(doc, "Класс",           "Эконом");
                AddRow(doc, "Место",           $"{10 + app.Id % 20}{(char)('A' + app.Id % 6)}");

                doc.Add(Chunk.NEWLINE);

                AddRow(doc, "Маршрут обратно", $"{country} → Москва (SVO)");
                AddRow(doc, "Дата вылета",     FormatDate(tour?.EndDate));
                AddRow(doc, "Рейс",            $"SU-{200 + app.Id % 900}");
                AddRow(doc, "Отправление",     "16:00");
                AddRow(doc, "Прибытие",        "19:30 (МСК)");

                doc.Add(Chunk.NEWLINE);
                doc.Add(new Chunk(new LineSeparator()));
                AddNote(doc, "Явка на регистрацию не позднее чем за 2 часа до вылета. Документ является электронным билетом.");
                AddNote(doc, $"Документ сформирован: {DateTime.Today:dd.MM.yyyy}");

                doc.Close();
            }
        }

        // ─── Страховой полис ─────────────────────────────────────────────────

        public static void GenerateInsurancePolicy(string path, TourApplication app)
        {
            var tour    = app.Tour;
            var hotel   = tour?.Hotels?.FirstOrDefault();
            var country = hotel?.Country?.Name ?? "страна тура";

            FileStream fs;
            var doc = CreateDoc(path, out fs);
            using (fs)
            {
                AddHeader(doc, "СТРАХОВОЙ ПОЛИС", $"Серия ТА № {app.Id:D8}");

                AddRow(doc, "Застрахованный",  FullName(app.User));
                AddRow(doc, "Страховщик",      "ООО «ТурАгентство Страхование»");
                AddRow(doc, "Вид страхования", "Туристическая страховка (выезд за рубеж)");
                AddRow(doc, "Страна покрытия", country);
                AddRow(doc, "Период действия", $"{FormatDate(tour?.StartDate)} – {FormatDate(tour?.EndDate)}");
                AddRow(doc, "Страховая сумма", "30 000 USD");
                AddRow(doc, "Франшиза",        "Отсутствует");
                AddRow(doc, "Покрытие",        "Медицинские расходы, эвакуация, несчастный случай");
                AddRow(doc, "Телефон помощи",  "+7 800 123-45-67 (круглосуточно, бесплатно)");

                doc.Add(Chunk.NEWLINE);
                doc.Add(new Chunk(new LineSeparator()));
                AddNote(doc, "Носите данный полис при себе в течение всей поездки.");
                AddNote(doc, $"Документ сформирован: {DateTime.Today:dd.MM.yyyy}");

                doc.Close();
            }
        }

        // ─── Виза ────────────────────────────────────────────────────────────

        public static void GenerateVisa(string path, TourApplication app)
        {
            var tour    = app.Tour;
            var hotel   = tour?.Hotels?.FirstOrDefault();
            var country = hotel?.Country?.Name ?? "страна тура";

            FileStream fs;
            var doc = CreateDoc(path, out fs);
            using (fs)
            {
                AddHeader(doc, "ВИЗА", $"Номер: ВЗ-{app.Id:D6}");

                AddRow(doc, "Владелец",          FullName(app.User));
                AddRow(doc, "Тип визы",           "Туристическая (однократная)");
                AddRow(doc, "Страна назначения",  country);
                AddRow(doc, "Действительна с",   FormatDate(tour?.StartDate));
                AddRow(doc, "Действительна по",  FormatDate(tour?.EndDate));
                AddRow(doc, "Кол-во въездов",    "1 (однократная)");
                AddRow(doc, "Срок пребывания",   tour?.StartDate.HasValue == true && tour.EndDate.HasValue
                    ? $"{(tour.EndDate.Value - tour.StartDate.Value).Days} дней"
                    : "по сроку тура");
                AddRow(doc, "Цель поездки",      "Туризм");
                AddRow(doc, "Выдана",            "Туристическим агентством 2018 на основании договора");

                doc.Add(Chunk.NEWLINE);
                doc.Add(new Chunk(new LineSeparator()));
                AddNote(doc, "Данный документ является подтверждением визовой поддержки. Оригинал визы вклеен в паспорт.");
                AddNote(doc, $"Документ сформирован: {DateTime.Today:dd.MM.yyyy}");

                doc.Close();
            }
        }
    }
}
