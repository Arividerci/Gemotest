using Gemotest;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;

namespace Laboratory.Gemotest
{
    public partial class FormGemotestResult : Form
    {
        private readonly GemotestAnalysisResult _result;

        public FormGemotestResult(GemotestAnalysisResult result)
        {
            _result = result ?? throw new ArgumentNullException(nameof(result));
            InitializeComponent();
            ConfigureGrids();
            BindData();
        }

        private void ConfigureGrids()
        {
            // Чтобы имена колонок были нормальные — задаём их кодом,
            // а внешний вид (шрифты/ширины) оставляем дизайнеру.
            gridCl.AutoGenerateColumns = false;
            gridCl.Columns.Clear();

            gridCl.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Раздел", DataPropertyName = "Section", FillWeight = 25 });
            gridCl.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Показатель", DataPropertyName = "Test", FillWeight = 20 });
            gridCl.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Результат", DataPropertyName = "Value", FillWeight = 15 });
            gridCl.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Ед. изм.", DataPropertyName = "Unit", FillWeight = 10 });
            gridCl.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Норма", DataPropertyName = "Reference", FillWeight = 15 });
            gridCl.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Статус", DataPropertyName = "Status", FillWeight = 10 });
            gridCl.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Дата", DataPropertyName = "Date", FillWeight = 10 });

            gridMb.AutoGenerateColumns = false;
            gridMb.Columns.Clear();

            gridMb.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Исследование", DataPropertyName = "Service", FillWeight = 25 });
            gridMb.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Материал", DataPropertyName = "Material", FillWeight = 15 });
            gridMb.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Локализация", DataPropertyName = "Localization", FillWeight = 15 });
            gridMb.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Результат", DataPropertyName = "Microbes", FillWeight = 35 });
            gridMb.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Статус", DataPropertyName = "Status", FillWeight = 10 });
        }

        private void BindData()
        {
            lblHeader.Text =
                $"Заказ: {_result.OrderNum}   Ext: {_result.ExtNum}   Дата: {(_result.DateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-")}   Статус: {MapOrderStatus(_result.Status)}";

            lblError.Text = _result.ErrorCode == 0 ? "Ошибок нет" : $"Ошибка: {_result.ErrorCode} {_result.ErrorDescription}";

            txtPdf.Text = _result.PdfUrl ?? "";
            btnSavePdf.Enabled = !string.IsNullOrWhiteSpace(_result.PdfUrl);

            // Группировка: одинаковый раздел показываем только в первой строке
            var ordered = _result.ClResults
                .OrderBy(x => x.SectionName)
                .ThenBy(x => x.TestRusName)
                .ToList();

            string lastSection = null;

            var clView = ordered.Select(x =>
            {
                var section = x.SectionName ?? "";
                var displaySection = (lastSection == section) ? "" : section;
                lastSection = section;

                var reference =
                    !string.IsNullOrWhiteSpace(x.RefRange) ? x.RefRange :
                    (!string.IsNullOrWhiteSpace(x.RefMin) || !string.IsNullOrWhiteSpace(x.RefMax)) ? $"{x.RefMin} - {x.RefMax}".Trim() :
                    (!string.IsNullOrWhiteSpace(x.RefText) ? x.RefText : "");

                return new
                {
                    Section = displaySection,
                    Test = x.TestRusName,
                    Value = x.Value,
                    Unit = x.MeasurementUnit,
                    Reference = reference,
                    Status = MapParamStatus(x.StatusCl),
                    Date = x.ResultDate
                };
            }).ToList();

            gridCl.DataSource = clView;

            var mbView = _result.MbServices.Select(s => new
            {
                Service = s.Name,
                Material = s.MatName,
                Localization = s.LocName,
                Microbes = string.Join("; ", s.Microbes.Select(m => $"{m.Id}={m.Value} (норма: {m.Norma})")),
                Status = MapMbStatus(s.StatusMb)
            }).ToList();

            gridMb.DataSource = mbView;
        }

        private static string MapOrderStatus(int code)
        {
            // Без официальной таблицы — не выдумываем.
            switch (code)
            {
                case 0: return "Нет результата";
                case 1: return "Результаты готовы";
                default: return $"Код {code}";
            }
        }

        private static string MapParamStatus(int code)
        {
            switch (code)
            {
                case 0: return "Нет данных";
                case 1: return "Готово";
                default: return $"Код {code}";
            }
        }

        private static string MapMbStatus(int code)
        {
            switch (code)
            {
                case 0: return "Нет данных";
                case 1: return "Готово";
                default: return $"Код {code}";
            }
        }

        private void btnSavePdf_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_result.PdfUrl))
                return;

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "PDF (*.pdf)|*.pdf";
                sfd.FileName = $"Gemotest_{_result.OrderNum}.pdf";

                if (sfd.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    using (var wc = new WebClient())
                    {
                        var data = wc.DownloadData(_result.PdfUrl);
                        File.WriteAllBytes(sfd.FileName, data);
                    }

                    MessageBox.Show("PDF сохранён.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка скачивания PDF: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
