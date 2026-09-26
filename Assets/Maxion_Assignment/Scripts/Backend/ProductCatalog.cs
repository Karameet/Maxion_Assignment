using System.Collections.Generic;

namespace MaxionAssignment.Backend
{
    public class ProductCatalog
    {
        readonly List<Product> products = new();

        public IReadOnlyList<Product> Products => products;

        public void Set(IEnumerable<Product> items)
        {
            products.Clear();
            if (items != null)
                products.AddRange(items);
        }

        public Product Find(string productId) => products.Find(p => p.id == productId);
    }
}
