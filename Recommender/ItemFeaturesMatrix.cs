using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recommender
{
    class ItemFeaturesMatrix : IItemFeaturesMatrix
    {
        /// <summary>internal data representation: list of sets representing the rows</summary>
        protected internal IList<IDictionary<int, UserItemData>> row_list = new List<IDictionary<int, UserItemData>>();
        //protected internal Dictionary<int, RatingDetails> itemDetails = new Dictionary<int, RatingDetails>();
        /// <summary>Indexer to access the elements of the matrix</summary>
        /// <param name="x">the row ID</param>
        /// <param name="y">the column ID</param>
        public UserItemData this[int x, int y]
        {
            get
            {
                if (x < row_list.Count)
                {
                    Dictionary<int, UserItemData> itemListForUser = (Dictionary<int, UserItemData>) row_list[x];
                    if (itemListForUser.ContainsKey(y))
                    {
                        return itemListForUser[y];
                    }
                }

                return null;
            }
            set
            {
                if (this[x] == null)
                    throw new Exception("<<<" + x + ">>>");

                if (value == null)
                {
                    this[x].Remove(y);
                }
                this[x].Add(y, value);
            }
        }

        public IDictionary<int, UserItemData> this[int x]
        {
            get
            {
                if (x >= row_list.Count)
                    for (int i = row_list.Count; i <= x; i++)
                        row_list.Add(new Dictionary<int, UserItemData>());

                return row_list[x];
            }
        }

        public int NumberOfEntries
        {
            get
            {
                int n = 0;
                foreach (var row in row_list)
                    n += row.Count;
                return n;
            }
        }

        public IList<int> GetEntriesByRow(int row_id)
        {
            return null;
        }

        public IDictionary<int, UserItemData> GetFeatureEntriesByRow(int row_id)
        {
            return row_list[row_id];
        }

        public IList<int> GetEntriesByColumn(int column_id)
        {
            var list = new List<int>();

            for (int row_id = 0; row_id < NumberOfRows; row_id++)
                if (row_list[row_id].ContainsKey(column_id))
                    list.Add(row_id);
            return list;
        }

        public IDictionary<int, UserItemData> GetFeatureEntriesByColumn(int column_id)
        {
            var list = new Dictionary<int, UserItemData>();

            for (int row_id = 0; row_id < NumberOfRows; row_id++)
                if (row_list[row_id].ContainsKey(column_id))
                    list.Add(row_id, row_list[row_id][column_id]);
            return list;
        }


        public MyMediaLite.DataType.IMatrix<UserItemData> CreateMatrix(int num_rows, int num_columns)
        {
            return new ItemFeaturesMatrix();
        }

        public bool IsSymmetric
        {
            get { throw new NotImplementedException(); }
        }

        public int NumberOfColumns
        {
            get
            {
                int max_column_id = -1;
                foreach (var row in row_list)
                    if (row.Count > 0)
                        max_column_id = Math.Max(max_column_id, row.Count);

                return max_column_id + 1;
            }
        }

        public int NumberOfRows
        {
            get { return row_list.Count; }
        }

        public void Resize(int num_rows, int num_cols)
        {
            throw new NotImplementedException();
        }

        public MyMediaLite.DataType.IMatrix<UserItemData> Transpose()
        {
            throw new NotImplementedException();
        }
    }
}
