using System.Data;

namespace Laboratory.Gemotest.Reports
{
    internal sealed class GemotestBlankReportDataSetV2
    {
        public DataTable BlankTable { get; private set; }
        public DataTable Products { get; private set; }
        public DataTable PatientParameters { get; private set; }

        public GemotestBlankReportDataSetV2()
        {
            BuildBlankTable();
            BuildProductsTable();
            BuildPatientParametersTable();
        }

        private void BuildBlankTable()
        {
            BlankTable = new DataTable("Blank");

            BlankTable.Columns.Add("DateofFormation", typeof(string));
            BlankTable.Columns.Add("DateSampling", typeof(string));
            BlankTable.Columns.Add("ClinicName", typeof(string));
            BlankTable.Columns.Add("OrderCode", typeof(string));
            BlankTable.Columns.Add("Pacient_FIO", typeof(string));
            BlankTable.Columns.Add("LaboratoryName", typeof(string));
            BlankTable.Columns.Add("OrderCodeBarcode", typeof(byte[]));
        }

        private void BuildProductsTable()
        {
            Products = new DataTable("Products");

            Products.Columns.Add("ProductCode", typeof(string));
            Products.Columns.Add("ProductName", typeof(string));
            Products.Columns.Add("SubOrderInfo", typeof(string));
        }

        private void BuildPatientParametersTable()
        {
            PatientParameters = new DataTable("PatientParams");

            PatientParameters.Columns.Add("PatientParamName", typeof(string));
            PatientParameters.Columns.Add("PatientParamValue", typeof(string));
        }
    }
}