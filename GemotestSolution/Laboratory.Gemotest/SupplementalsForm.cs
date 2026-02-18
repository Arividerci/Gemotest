using Laboratory.Gemotest.GemotestRequests;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Laboratory.Gemotest
{
    public partial class SupplementalsForm : Form
    {
        private readonly DataGridView _grid = new DataGridView();
        private readonly Button _ok = new Button();
        private readonly Button _cancel = new Button();

        public Dictionary<string, string> Values { get; private set; }

        public SupplementalsForm(List<DictionaryServicesSupplementals> items)
        {
            Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            Text = "Дополнительные поля услуг";
            Width = 900;
            Height = 450;
            StartPosition = FormStartPosition.CenterParent;

            _grid.Dock = DockStyle.Top;
            _grid.Height = 340;
            _grid.AllowUserToAddRows = false;
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            _grid.Columns.Add("test_id", "Код");
            _grid.Columns.Add("name", "Название");
            _grid.Columns.Add("value", "Значение");

            for (int i = 0; i < items.Count; i++)
                _grid.Rows.Add(items[i].test_id, items[i].name, "");

            _ok.Text = "OK";
            _ok.Left = 650;
            _ok.Top = 350;
            _ok.Width = 100;
            _ok.Click += (s, e) =>
            {
                Values.Clear();
                foreach (DataGridViewRow r in _grid.Rows)
                {
                    var id = (r.Cells[0].Value ?? "").ToString().Trim();
                    var val = (r.Cells[2].Value ?? "").ToString().Trim();
                    if (id.Length > 0)
                        Values[id] = val;
                }
                DialogResult = DialogResult.OK;
                Close();
            };

            _cancel.Text = "Отмена";
            _cancel.Left = 760;
            _cancel.Top = 350;
            _cancel.Width = 100;
            _cancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.Add(_grid);
            Controls.Add(_ok);
            Controls.Add(_cancel);
        }
    }
}