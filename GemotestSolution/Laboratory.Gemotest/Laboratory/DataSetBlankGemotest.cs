using System;
using System.Data;

namespace Laboratory.Gemotest
{
    public sealed class DataSetBlankGemotest
    {
        public DataTable BlankTable { get; private set; }
        public DataTable Products { get; private set; }
        public DataTable PatientParameters { get; private set; }

        public DataSetBlankGemotest()
        {
            BuildBlankTable();
            BuildProductsTable();
            BuildPatientParametersTable();
        }

        private void BuildBlankTable()
        {
            BlankTable = new DataTable("Blank");

            BlankTable.Columns.Add("DateofFormation", typeof(string));
            BlankTable.Columns.Add("LaboratoryName", typeof(string));
            BlankTable.Columns.Add("ClinicName", typeof(string));
            BlankTable.Columns.Add("OrderCode", typeof(string));
            BlankTable.Columns.Add("Pacient_FIO", typeof(string));
            BlankTable.Columns.Add("PriceListName", typeof(string));
        }

        private void BuildProductsTable()
        {
            Products = new DataTable("Products");

            Products.Columns.Add("PROD_CODE", typeof(string));
            Products.Columns.Add("PROD_NAME", typeof(string));

            Products.Columns.Add("SAMPLE_BARCODE", typeof(string));
            Products.Columns.Add("SAMPLE_ID", typeof(string));
            Products.Columns.Add("SAMPLE_IDENTIFIER", typeof(string));

            Products.Columns.Add("BIOMATERIAL_NAME", typeof(string));
            Products.Columns.Add("CONTAINER_NAME", typeof(string));
            Products.Columns.Add("LOCALIZATION_NAME", typeof(string));
            Products.Columns.Add("TRANSPORT_NAME", typeof(string));
            Products.Columns.Add("LAB_CENTER_ID", typeof(string));
            Products.Columns.Add("SAMPLE_DESCRIPTION", typeof(string));
        }

        private void BuildPatientParametersTable()
        {
            PatientParameters = new DataTable("PatientParams");

            PatientParameters.Columns.Add("PatientParamName", typeof(string));
            PatientParameters.Columns.Add("PatientParamValue", typeof(string));
        }
    }
}