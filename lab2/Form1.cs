using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Configuration;
using System.Collections.Generic;
using System.Drawing;

namespace lab1
{
    public partial class Form1 : Form
    {
        string connectionString;
        SqlConnection cs;
        SqlDataAdapter da = new SqlDataAdapter();
        DataSet dsParent = new DataSet();
        DataSet dsChild = new DataSet();

        public Form1()
        {
            InitializeComponent();

            connectionString = ConfigurationManager.ConnectionStrings["cn"].ConnectionString;
            cs = new SqlConnection(connectionString);

            CreateDynamicControls();

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void CreateDynamicControls()
        {
            panel1.Controls.Clear(); // Clear existing controls

            // Read column names from App.config
            string[] columnNames = ConfigurationManager.AppSettings["ChildColumnNames"].Split(',');

            int yPos = 10; // Vertical position for controls

            foreach (string columnName in columnNames)
            {
                // Create Label
                Label lbl = new Label();
                lbl.Text = columnName;
                lbl.Location = new Point(10, yPos);
                lbl.Width = 80;
                panel1.Controls.Add(lbl);

                // Create TextBox
                TextBox txt = new TextBox();
                txt.Name = "txt" + columnName; // e.g., "txtCuloare"
                txt.Location = new Point(100, yPos);
                txt.Width = 150;
                panel1.Controls.Add(txt);

                yPos += 30; // Move down for the next control
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                cs.Open();
                // Load parent table (GhemBumbac)
                string selectParentTable = ConfigurationManager.AppSettings["SelectParentTable"];
                da.SelectCommand = new SqlCommand(selectParentTable, cs);
                dsParent.Clear();
                da.Fill(dsParent);
                dataGridView1.DataSource = dsParent.Tables[0];

                // Load child table (FirBumbac)
                string selectChildTable = ConfigurationManager.AppSettings["SelectChildTable"];
                da.SelectCommand = new SqlCommand(selectChildTable, cs);
                dsChild.Clear();
                da.Fill(dsChild);
                dataGridView2.DataSource = dsChild.Tables[0];

                // Set up the selection changed event
                dataGridView1.SelectionChanged -= DataGridView1_SelectionChanged;
                dataGridView1.SelectionChanged += DataGridView1_SelectionChanged;

                dataGridView2.SelectionChanged -= DataGridView2_SelectionChanged;
                dataGridView2.SelectionChanged += DataGridView2_SelectionChanged;

                // Select first row by default
                if (dataGridView1.Rows.Count > 0)
                {
                    dataGridView1.Rows[0].Selected = true;
                    DataGridView1_SelectionChanged(null, null);
                }

                MessageBox.Show("Data loaded successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }

        private void DataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.SelectedRows.Count > 0)
                {
                    DataGridViewRow selectedRow = dataGridView1.SelectedRows[0];
                    string ChildForeignKey = ConfigurationManager.AppSettings["ChildForeignKey"];

                    // Check if Gid column exists and has value
                    if (selectedRow.Cells[ChildForeignKey] != null && selectedRow.Cells[ChildForeignKey].Value != null)
                    {
                        int selectedForeignKey = Convert.ToInt32(selectedRow.Cells[ChildForeignKey].Value);

                        // Filter child records
                        DataView dv = new DataView(dsChild.Tables[0]);
                        dv.RowFilter = $"{ChildForeignKey} = {selectedForeignKey}";
                        dataGridView2.DataSource = dv;

                        //Fill the foreign key textbox
                        foreach (Control ctrl in panel1.Controls)
                        {
                            if (ctrl is TextBox txt)
                            {
                                string columnName = txt.Name.Substring(3); // Remove "txt" prefix
                                if (columnName == ChildForeignKey)
                                {
                                    txt.Text = selectedRow.Cells[ChildForeignKey].Value.ToString();
                                }
                                else
                                {
                                    txt.Text = string.Empty; // Clear if no value
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error filtering data: {ex.Message}");
            }
        }

        private void DataGridView2_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView2.SelectedRows.Count > 0)
                {
                    DataGridViewRow selectedRow = dataGridView2.SelectedRows[0];
                    string ChildPrimaryKey = ConfigurationManager.AppSettings["ChildPrimaryKey"];

                    // Check if Fid column exists and has value
                    if (selectedRow.Cells[ChildPrimaryKey] != null && selectedRow.Cells[ChildPrimaryKey].Value != null)
                    {
                        // fill the textboxes with the selected row's data
                        foreach (Control ctrl in panel1.Controls)
                        {
                            if (ctrl is TextBox txt)
                            {
                                string columnName = txt.Name.Substring(3); // Remove "txt" prefix
                                if (selectedRow.Cells[columnName] != null && selectedRow.Cells[columnName].Value != null)
                                {
                                    txt.Text = selectedRow.Cells[columnName].Value.ToString();
                                }
                                else
                                {
                                    txt.Text = string.Empty; // Clear if no value
                                }

                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }
        private void buttonAddClick(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView1.SelectedRows.Count > 0)
                {
                    string ChildForeignKey = ConfigurationManager.AppSettings["ChildForeignKey"];
                    string ChildPrimaryKey = ConfigurationManager.AppSettings["ChildPrimaryKey"];
                    int selectedForeignKey = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells[ChildForeignKey].Value);

                    // Validate inputs
                    foreach (Control ctrl in panel1.Controls)
                    {
                        if (ctrl is TextBox txt)
                        {
                            if (string.IsNullOrWhiteSpace(txt.Text))
                                throw new Exception($"{txt.Name} cannot be empty");
                        }
                    }

                    // Insert new record
                    string commandText = ConfigurationManager.AppSettings["InsertQuery"];
                    da.InsertCommand = new SqlCommand(commandText, cs);
                    List<string> strings = new List<string>(ConfigurationManager.AppSettings["ColumnNamesInsertParameters"].Split(','));

                    //check if the primary key is in the commandText 
                    if (commandText.Contains(ChildPrimaryKey))
                    {
                        da.InsertCommand.Parameters.Add($"{strings[0]}", SqlDbType.Int).Value = dsChild.Tables[0].Rows.Count + 1;
                    }

                    foreach (string column in strings)
                    {
                        string columnName = column.TrimStart('@');
                        if (columnName != ChildPrimaryKey) 
                        {
                            da.InsertCommand.Parameters.Add($"{column}", SqlDbType.VarChar).Value = panel1.Controls.Find("txt" + columnName, true)[0].Text;
                        }
                    }

                    da.InsertCommand.ExecuteNonQuery();

                    // Refresh data
                    dsChild.Clear();
                    da.Fill(dsChild);
                    DataGridView1_SelectionChanged(null, null); // Reapply filter

                    MessageBox.Show("Record added successfully");

                    // Clear textboxes
                    foreach (Control ctrl in panel1.Controls)
                    {
                        if (ctrl is TextBox txt)
                        {
                            txt.Text = string.Empty;
                        }
                    }

                }
                else
                {
                    MessageBox.Show("Please select a parent record first");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
                if (cs.State == ConnectionState.Open) cs.Close();
            }
        }

        private void buttonUpdateClick(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView2.SelectedRows.Count > 0)
                {
                    string ChildPrimaryKey = ConfigurationManager.AppSettings["ChildPrimaryKey"];
                    int selectedChildPrimaryKey = Convert.ToInt32(dataGridView2.SelectedRows[0].Cells[ChildPrimaryKey].Value);

                    // Validate inputs
                    foreach (Control ctrl in panel1.Controls)
                    {
                        if (ctrl is TextBox txt)
                        {
                            if (string.IsNullOrWhiteSpace(txt.Text))
                                throw new Exception($"{txt.Name} cannot be empty");
                        }
                    }

                    // Update record
                    string commandText = ConfigurationManager.AppSettings["UpdateQuery"];
                    List<string> ColumnParameters = new List<string>(ConfigurationManager.AppSettings["ColumnNamesUpdateParameters"].Split(','));
                    da.UpdateCommand = new SqlCommand(commandText, cs);
                    foreach (string column in ColumnParameters)
                    {
                        da.UpdateCommand.Parameters.Add($"{column}", SqlDbType.VarChar).Value = panel1.Controls.Find("txt" + column.TrimStart('@'), true)[0].Text;
                    }
                    da.UpdateCommand.Parameters.Add($"@{ChildPrimaryKey}", SqlDbType.Int).Value = selectedChildPrimaryKey;
                    da.UpdateCommand.ExecuteNonQuery();

                    // Refresh data
                    dsChild.Clear();
                    da.Fill(dsChild);
                    DataGridView1_SelectionChanged(null, null); // Reapply filter

                    MessageBox.Show("Record updated successfully");
                }
                else
                {
                    MessageBox.Show("Please select a record first");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
                if (cs.State == ConnectionState.Open) cs.Close();
            }
        }

        private void buttonDeleteClick(object sender, EventArgs e)
        {
            try
            {
                if (dataGridView2.SelectedRows.Count > 0)
                {
                    string ChildPrimaryKey = ConfigurationManager.AppSettings["ChildPrimaryKey"];
                    int selectedChildPrimaryKey = Convert.ToInt32(dataGridView2.SelectedRows[0].Cells[ChildPrimaryKey].Value);

                    // Delete record
                    string commandText = ConfigurationManager.AppSettings["DeleteQuery"];
                    da.DeleteCommand = new SqlCommand(commandText, cs);
                    da.DeleteCommand.Parameters.Add($"{ChildPrimaryKey}", SqlDbType.Int).Value = selectedChildPrimaryKey;
                    da.DeleteCommand.ExecuteNonQuery();

                    // Refresh data
                    dsChild.Clear();
                    da.Fill(dsChild);
                    DataGridView1_SelectionChanged(null, null); // Reapply filter

                    MessageBox.Show("Record deleted successfully");
                }
                else
                {
                    MessageBox.Show("Please select a record first");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
                if (cs.State == ConnectionState.Open) cs.Close();
            }
        }
    }
}