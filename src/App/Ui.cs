using QuanLyChuoiCafe.Domain.Exceptions;

namespace QuanLyChuoiCafe.App;

/// <summary>Quy ước giao diện dùng chung cho mọi form (cả hai phân hệ).</summary>
public static class Ui
{
    public static readonly Color Primary = Color.FromArgb(111, 78, 55);
    public static readonly Color Background = Color.FromArgb(250, 247, 242);
    public static readonly Color Soft = Color.FromArgb(236, 226, 214);

    public static void Prepare(Form form, string title, int width, int height)
    {
        form.AutoScaleDimensions = new SizeF(96F, 96F);
        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.Text = title;
        form.ClientSize = new Size(width, height);
        form.Font = new Font("Segoe UI", 10F);
        form.BackColor = Background;
        form.StartPosition = FormStartPosition.CenterScreen;
    }

    public static Button Button(string text, EventHandler onClick, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            MinimumSize = new Size(96, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Primary : Color.White,
            ForeColor = primary ? Color.White : Primary,
            Margin = new Padding(4)
        };
        button.FlatAppearance.BorderColor = Primary;
        button.Click += onClick;
        return button;
    }

    public static Label Label(string text, bool bold = false) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(4, 8, 4, 4),
        Font = new Font("Segoe UI", 10F, bold ? FontStyle.Bold : FontStyle.Regular)
    };

    public static FlowLayoutPanel Row(params Control[] controls)
    {
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
        panel.Controls.AddRange(controls);
        return panel;
    }

    public static DataGridView Grid(params (string Name, string Header, int Weight, string Format)[] columns)
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        };
        grid.ColumnHeadersDefaultCellStyle.BackColor = Soft;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Soft;
        grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(181, 148, 120);

        foreach (var (name, header, weight, format) in columns)
        {
            var column = new DataGridViewTextBoxColumn { Name = name, HeaderText = header, FillWeight = weight };
            if (format != null)
            {
                column.DefaultCellStyle.Format = format;
                column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            grid.Columns.Add(column);
        }
        return grid;
    }

    /// <summary>Chạy một thao tác; lỗi nghiệp vụ hiện cảnh báo thân thiện.</summary>
    public static bool Try(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch (DomainException ex)
        {
            MessageBox.Show(ex.Message, "Không thực hiện được", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }
    }

    public static void Info(string message) =>
        MessageBox.Show(message, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
}
