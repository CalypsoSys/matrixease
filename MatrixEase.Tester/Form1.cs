using MatrixEase.Manga.Manga.Serialization;
using MatrixEase.Manga.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.ListViewItem;

namespace MatrixEase.Tester
{
    public partial class Form1 : Form
    {
        private const string _specFile = @"C:\CalypsoSystems\mangadata\matrixease\tester\tester.spec";
        private const string _testsPath = @"C:\CalypsoSystems\mangadata\matrixease\tester\tests";

        public Form1()
        {
            InitializeComponent();
            foreach (var row in Specs())
            {
                _testsLst.Items.Add(new ListViewItem(row.ToArray()));
            }
        }

        private void _browseBtn_Click(object sender, EventArgs e)
        {
            if (_openFileDialog.ShowDialog(this) == DialogResult.OK)
            {
                _spec1Txt.Text = _openFileDialog.FileName;
            }
        }

        private void _addBtn_Click(object sender, EventArgs e)
        {
            List<string> parts = new List<string>();
            parts.Add(_typeCmb.SelectedItem as string);
            parts.Add(_sepTxt.Text);
            parts.Add(_spec1Txt.Text);
            parts.Add(_spec2Txt.Text);

            if (string.IsNullOrWhiteSpace(parts[0]) || string.IsNullOrWhiteSpace(parts[2]) || parts[1].Length > 1)
            {
                MessageBox.Show("Invalid Entries, try again");
            }
            else
            {
                _testsLst.Items.Add(new ListViewItem(parts.ToArray()));
            }
        }

        private void _saveBtn_Click(object sender, EventArgs e)
        {
            using (StreamWriter spec = new StreamWriter(_specFile))
            {
                foreach(ListViewItem item in _testsLst.Items)
                {
                    List<string> parts = new List<string>();
                    foreach (ListViewSubItem cell in item.SubItems)
                    {
                        spec.Write("\"{0}\",", cell.Text);
                    }
                    spec.WriteLine();
                }
            }
        }

        private void _runBtn_Click(object sender, EventArgs e)
        {
            MangaRoot.SetRootFolder(_testsPath);

            foreach (var row in Specs())
            {
            }
        }

        private IEnumerable<IList<string>> Specs()
        {
            using (StreamReader spec = new StreamReader(_specFile))
            {
                using (CsvParser parser = new CsvParser(spec, ',', '"', '"', '\0', '\r', '\n'))
                {
                    foreach (var row in parser.ReadParseLine())
                    {
                        List<string> parts = new List<string>();
                        foreach (string cell in row)
                        {
                            parts.Add(cell);
                        }
                        yield return parts;
                    }
                }
            }
        }
    }
}
