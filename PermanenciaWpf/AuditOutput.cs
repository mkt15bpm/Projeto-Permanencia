using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace PermanenciaWpf;

public static class AuditOutput
{
    public static void Export(Window owner, IEnumerable<JsonObject> entries)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PDF|*.pdf",
            FileName = "AUDITORIA_ULTIMOS_30_DIAS_" + DateTime.Today.ToString("dd-MM-yyyy") + ".pdf"
        };
        if (dialog.ShowDialog(owner) != true) return;
        VisualPdfWriter.Save(Pages(entries), dialog.FileName);
        MessageBox.Show("Relatório de auditoria gerado.");
    }

    private static List<Border> Pages(IEnumerable<JsonObject> source)
    {
        var entries = source.ToList();
        var document = new FlowDocument
        {
            PageWidth = 794,
            PageHeight = 1123,
            PagePadding = new Thickness(40),
            ColumnWidth = 714,
            FontFamily = new FontFamily("Arial"),
            FontSize = 10,
            Foreground = Brushes.Black,
            Background = Brushes.White
        };
        Paragraph P(string text, bool bold = false) => new(new Run(text))
        {
            FontWeight = bold ? FontWeights.Bold : FontWeights.Normal,
            Margin = new Thickness(0, 2, 0, 7)
        };

        var heading = new Table { CellSpacing = 0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(.7) };
        heading.Columns.Add(new TableColumn { Width = new GridLength(160) });
        heading.Columns.Add(new TableColumn { Width = new GridLength(394) });
        heading.Columns.Add(new TableColumn { Width = new GridLength(160) });
        var group = new TableRowGroup();
        heading.RowGroups.Add(group);
        var row = new TableRow();
        group.Rows.Add(row);
        row.Cells.Add(new TableCell(P("15º BATALHÃO\nPOLÍCIA MILITAR", true)) { TextAlignment = TextAlignment.Center, Padding = new Thickness(8), FontSize = 9 });
        var center = new TableCell { TextAlignment = TextAlignment.Center, Padding = new Thickness(8) };
        center.Blocks.Add(new BlockUIContainer(UI.Crest(55)));
        center.Blocks.Add(P("RELATÓRIO DE AUDITORIA", true));
        center.Blocks.Add(P("ÚLTIMOS 30 DIAS"));
        row.Cells.Add(center);
        row.Cells.Add(new TableCell(P("GERADO EM\n" + DateTime.Now.ToString("dd/MM/yyyy HH:mm"))) { TextAlignment = TextAlignment.Center, Padding = new Thickness(8), FontSize = 9 });
        document.Blocks.Add(heading);
        document.Blocks.Add(P("Histórico completo das alterações registradas no sistema no período de " + DateTime.Today.AddDays(-30).ToString("dd/MM/yyyy") + " a " + DateTime.Today.ToString("dd/MM/yyyy") + "."));
        document.Blocks.Add(P("Total de registros: " + entries.Count + ".", true));

        var table = new Table { CellSpacing = 0, BorderBrush = Brushes.Black, BorderThickness = new Thickness(.5), FontSize = 8.5, Margin = new Thickness(0, 5, 0, 8) };
        foreach (var width in new[] { 95d, 74d, 112d, 75d, 90d, 268d }) table.Columns.Add(new TableColumn { Width = new GridLength(width) });
        var rows = new TableRowGroup();
        table.RowGroups.Add(rows);
        AddRow(new[] { "DATA / HORA", "USUÁRIO", "AÇÃO", "QUARTEL", "RELATÓRIO", "DETALHES" }, true);
        if (entries.Count == 0) AddRow(new[] { "NENHUM REGISTRO ENCONTRADO", "", "", "", "", "" }, false);
        else foreach (var entry in entries)
        {
            var date = DateTime.TryParse(entry.S("at"), out var parsed) ? parsed.ToString("dd/MM/yyyy\nHH:mm") : entry.S("at");
            AddRow(new[] { date, entry.S("user"), entry.S("action"), entry.S("unit"), entry.S("report"), entry.S("details") }, false);
        }
        document.Blocks.Add(table);

        var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
        paginator.ComputePageCount();
        var pages = new List<Border>();
        for (var i = 0; i < paginator.PageCount; i++)
        {
            var page = paginator.GetPage(i);
            var grid = new Grid { Width = 794, Height = 1123, Background = Brushes.White };
            grid.Children.Add(new System.Windows.Shapes.Rectangle { Fill = new VisualBrush(page.Visual) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top } });
            grid.Children.Add(new TextBlock { Text = $"Auditoria • Página {i + 1} de {paginator.PageCount}", FontSize = 10, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(30, 0, 40, 18), Foreground = Brushes.Black });
            var border = new Border { Child = grid, Width = 794, Height = 1123, Background = Brushes.White };
            border.Measure(new Size(794, 1123));
            border.Arrange(new Rect(0, 0, 794, 1123));
            border.UpdateLayout();
            pages.Add(border);
        }
        return pages;

        void AddRow(string[] values, bool header)
        {
            var current = new TableRow();
            foreach (var value in values)
            {
                var cell = new TableCell(P(value, header))
                {
                    BorderBrush = Brushes.Black,
                    BorderThickness = new Thickness(.5),
                    Padding = new Thickness(4),
                    Background = header ? new SolidColorBrush(Color.FromRgb(224, 232, 242)) : Brushes.White,
                    TextAlignment = header ? TextAlignment.Center : TextAlignment.Left
                };
                current.Cells.Add(cell);
            }
            rows.Rows.Add(current);
        }
    }
}
