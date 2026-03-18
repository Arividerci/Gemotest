using System;
using System.Data;

namespace Laboratory.Gemotest.Reports
{
    internal sealed class DataSetForReportGemotest
    {
        public DataTable ConsolidatedReportHeader { get; private set; }
        public DataTable ConsolidatedReportParameters { get; private set; }

        public DataSetForReportGemotest()
        {
            BuildHeaderTable();
            BuildParametersTable();
        }

        private void BuildHeaderTable()
        {
            ConsolidatedReportHeader = new DataTable("ConsolidatedReportHeader");

            ConsolidatedReportHeader.Columns.Add("ORDER_DATE", typeof(DateTime));
            ConsolidatedReportHeader.Columns.Add("ORDER_NUMBER", typeof(string));
            ConsolidatedReportHeader.Columns.Add("PER_FIO", typeof(string));
            ConsolidatedReportHeader.Columns.Add("PER_BORN_DATE", typeof(DateTime));
            ConsolidatedReportHeader.Columns.Add("PER_SEX", typeof(string));
        }

        private void BuildParametersTable()
        {
            ConsolidatedReportParameters = new DataTable("ConsolidatedReportParameters");

            ConsolidatedReportParameters.Columns.Add("PROD_CODE", typeof(string));
            ConsolidatedReportParameters.Columns.Add("PROD_NAME", typeof(string));
            ConsolidatedReportParameters.Columns.Add("TEST_NAME", typeof(string));

            ConsolidatedReportParameters.Columns.Add("PARAM_NUMBER", typeof(int));
            ConsolidatedReportParameters.Columns.Add("PARAM_NAME", typeof(string));
            ConsolidatedReportParameters.Columns.Add("PARAM_UNIT", typeof(string));
            ConsolidatedReportParameters.Columns.Add("PARAM_VALUE_TEXT", typeof(string));
            ConsolidatedReportParameters.Columns.Add("PARAM_REF_TEXT", typeof(string));

            ConsolidatedReportParameters.Columns.Add("PARAM_VALUE", typeof(double));
            ConsolidatedReportParameters.Columns.Add("PARAM_MIN", typeof(double));
            ConsolidatedReportParameters.Columns.Add("PARAM_MAX", typeof(double));
        }
    }
}