using Laboratory.Gemotest.GemotestRequests;
using Laboratory.Gemotest.SourseClass;
using SiMed.Laboratory;
using StatisticsCollectionSystemClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;

namespace Laboratory.Gemotest.GUI
{
    public partial class GemotestChoiceOfProductForm : Form
    {
        public class ProductInfo
        {
            public string Id { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }
        }
        Dictionaries Dicts;
        public SiMed.Laboratory.Product SelectedProduct;
        SiMed.Laboratory.Product SourceProduct;
        Dictionary<string, DictionaryService> Directory;
        List<ProductInfo> Products = new List<ProductInfo>();
        List<ProductGemotest> Product;
        BindingSource bsProducts = null;

        public GemotestChoiceOfProductForm(SiMed.Laboratory.Product sourceProduct, List<ProductGemotest> product)
        {
            InitializeComponent();
            SourceProduct = sourceProduct;
            Product = product;
        }
        private void tbFilter_TextChanged(object sender, EventArgs e)
        {
            SetFilterForProducts();
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            SourceProduct = null;
            DialogResult = DialogResult.Cancel;
        }

        private void btGroupFilterClear_Click(object sender, EventArgs e)
        {
            SetFilterForProducts();
        }

        private void btTextFilterClear_Click(object sender, EventArgs e)
        {
            tbFilter.Text = "";
            SetFilterForProducts();
        }

        private void SetFilterForProducts()
        {
            List<ProductInfo> ProductsFiltered = Products;

            if (tbFilter.Text != "")
                ProductsFiltered = Products.Where(x => x.Code.Contains(tbFilter.Text) || x.Name.Contains(tbFilter.Text)).ToList();

            bsProducts = new BindingSource(ProductsFiltered, null);
            dgvProducts.DataSource = bsProducts;
        }

        private void bOK_Click(object sender, EventArgs e)
        {
            if (bsProducts.Current == null)
            {
                MessageBox.Show("Необходимо выбрать продукт");
                return;
            }
            ProductInfo productInfo = (ProductInfo)bsProducts.Current;
            SelectedProduct = new SiMed.Laboratory.Product() { ID = productInfo.Id, Code = productInfo.Code, Name = productInfo.Name };
            DialogResult = DialogResult.OK;
        }

        private void dgvProducts_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            bOK_Click(null, null);
        }


        private void GemotestChoiceOfProductForm_Load(object sender, EventArgs e)
        {
            foreach (var item in Product)
            {
                Products.Add(new ProductInfo()
                {
                    Id = item.ID,
                    Code = item.Code,
                    Name = item.Name,
                });
            }

            Products = Products.OrderBy(x => x.Name).ToList();

            bsProducts = new BindingSource(Products, null);

            dgvProducts.DataSource = bsProducts;

            if (SourceProduct != null)
            {
                bsProducts.MoveFirst();
                for (int i = 0; i < bsProducts.Count; i++)
                {
                    if (((ProductInfo)bsProducts.Current).Code == SourceProduct.Code)
                        break;
                    bsProducts.MoveNext();
                }
            }

        }
    
    }
}
