using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace lab1
{
    public partial class Form1 : Form
    {
        SqlConnection cs = new SqlConnection("Data Source=DESKTOP-MMVJJUN\\SQLEXPRESS;Initial Catalog=MagazinMercerie;Integrated Security=True");
        SqlDataAdapter da = new SqlDataAdapter();
        DataSet dsGhem = new DataSet();
        DataSet dsFir = new DataSet();

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                // Load parent table (GhemBumbac)
                da.SelectCommand = new SqlCommand("SELECT * FROM GhemBumbac", cs);
                dsGhem.Clear();
                da.Fill(dsGhem);
                dataGridView1.DataSource = dsGhem.Tables[0];

                // Load child table (FirBumbac)
                da.SelectCommand = new SqlCommand("SELECT * FROM FirBumbac", cs);
                dsFir.Clear();
                da.Fill(dsFir);
                dataGridView2.DataSource = dsFir.Tables[0];

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

                    // Check if Gid column exists and has value
                    if (selectedRow.Cells["Gid"] != null && selectedRow.Cells["Gid"].Value != null)
                    {
                        int selectedGid = Convert.ToInt32(selectedRow.Cells["Gid"].Value);

                        // Update the Gid textbox
                        textBox4.Text = selectedGid.ToString();

                        // Filter child records
                        DataView dv = new DataView(dsFir.Tables[0]);
                        dv.RowFilter = $"Gid = {selectedGid}";
                        dataGridView2.DataSource = dv;
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

                    // Check if Fid column exists and has value
                    if (selectedRow.Cells["Fid"] != null && selectedRow.Cells["Fid"].Value != null)
                    {
                        textBox1.Text = selectedRow.Cells["Culoare"].Value.ToString();
                        textBox2.Text = selectedRow.Cells["Lungime"].Value.ToString();
                        textBox3.Text = selectedRow.Cells["Grosime"].Value.ToString();
                        textBox4.Text = selectedRow.Cells["Gid"].Value.ToString();
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
                    int selectedGid = Convert.ToInt32(dataGridView1.SelectedRows[0].Cells["Gid"].Value);

                    // Validate inputs
                    if (string.IsNullOrWhiteSpace(textBox1.Text))
                        throw new Exception("Culoare cannot be empty");
                    if (!int.TryParse(textBox2.Text, out int lungime))
                        throw new Exception("Lungime must be a number");
                    if (!int.TryParse(textBox3.Text, out int grosime))
                        throw new Exception("Grosime must be a number");

                    // Insert new record
                    da.InsertCommand = new SqlCommand(
                        "INSERT INTO FirBumbac (Fid, Culoare, Lungime, Grosime, Gid) VALUES (@f, @c, @l, @g, @gid)",
                        cs);

                    // Generate new Fid
                    da.InsertCommand.Parameters.Add("@f", SqlDbType.Int).Value = dsFir.Tables[0].Rows.Count + 1;
                    da.InsertCommand.Parameters.Add("@c", SqlDbType.VarChar).Value = textBox1.Text;
                    da.InsertCommand.Parameters.Add("@l", SqlDbType.Int).Value = lungime;
                    da.InsertCommand.Parameters.Add("@g", SqlDbType.Int).Value = grosime;
                    da.InsertCommand.Parameters.Add("@gid", SqlDbType.Int).Value = selectedGid;

                    cs.Open();
                    da.InsertCommand.ExecuteNonQuery();
                    cs.Close();

                    // Refresh data
                    dsFir.Clear();
                    da.Fill(dsFir);
                    DataGridView1_SelectionChanged(null, null); // Reapply filter

                    MessageBox.Show("Record added successfully");

                    // Clear input fields
                    textBox1.Text = "Culoare";
                    textBox2.Text = "Lungime";
                    textBox3.Text = "Grosime";
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
                    int selectedFid = Convert.ToInt32(dataGridView2.SelectedRows[0].Cells["Fid"].Value);

                    // Validate inputs
                    if (string.IsNullOrWhiteSpace(textBox1.Text))
                        throw new Exception("Culoare cannot be empty");
                    if (!int.TryParse(textBox2.Text, out int lungime))
                        throw new Exception("Lungime must be a number");
                    if (!int.TryParse(textBox3.Text, out int grosime))
                        throw new Exception("Grosime must be a number");

                    // Update record
                    da.UpdateCommand = new SqlCommand(
                        "UPDATE FirBumbac SET Culoare = @c, Lungime = @l, Grosime = @g WHERE Fid = @f",
                        cs);

                    da.UpdateCommand.Parameters.Add("@c", SqlDbType.VarChar).Value = textBox1.Text;
                    da.UpdateCommand.Parameters.Add("@l", SqlDbType.Int).Value = lungime;
                    da.UpdateCommand.Parameters.Add("@g", SqlDbType.Int).Value = grosime;
                    da.UpdateCommand.Parameters.Add("@f", SqlDbType.Int).Value = selectedFid;

                    cs.Open();
                    da.UpdateCommand.ExecuteNonQuery();
                    cs.Close();

                    // Refresh data
                    dsFir.Clear();
                    da.Fill(dsFir);
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
                    int selectedFid = Convert.ToInt32(dataGridView2.SelectedRows[0].Cells["Fid"].Value);

                    // Delete record
                    da.DeleteCommand = new SqlCommand("DELETE FROM FirBumbac WHERE Fid = @f", cs);
                    da.DeleteCommand.Parameters.Add("@f", SqlDbType.Int).Value = selectedFid;

                    cs.Open();
                    da.DeleteCommand.ExecuteNonQuery();
                    cs.Close();

                    // Refresh data
                    dsFir.Clear();
                    da.Fill(dsFir);
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