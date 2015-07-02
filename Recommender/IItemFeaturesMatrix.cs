using MyMediaLite.DataType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recommender
{
    interface IItemFeaturesMatrix : IMatrix<UserItemData>
    {
        /// <summary>Indexer to access the rows of the matrix</summary>
        /// <param name="x">the row ID</param>
        IDictionary<int, UserItemData> this[int x] { get; }

        /// <summary>The number of (true) entries</summary>
        int NumberOfEntries { get; }

        /// <summary>The IDs of the non-empty rows in the matrix (the ones that contain at least one true entry)</summary>
        //IList<int> NonEmptyRowIDs { get; }

        /// <summary>The IDs of the non-empty columns in the matrix (the ones that contain at least one true entry)</summary>
        //IList<int> NonEmptyColumnIDs { get; }

        /// <summary>Get all true entries (column IDs) of a row</summary>
        /// <param name="row_id">the row ID</param>
        /// <returns>a list of column IDs</returns>
        IList<int> GetEntriesByRow(int row_id);

        IDictionary<int, UserItemData> GetFeatureEntriesByRow(int row_id);

        /// <summary>Get all the number of entries in a row</summary>
        /// <param name="row_id">the row ID</param>
        /// <returns>the number of entries in row row_id</returns>
        //int NumEntriesByRow(int row_id);

        /// <summary>Get all true entries (row IDs) of a column</summary>
        /// <param name="column_id">the column ID</param>
        /// <returns>a list of row IDs</returns>
        IList<int> GetEntriesByColumn(int column_id);

        IDictionary<int, UserItemData> GetFeatureEntriesByColumn(int column_id);

        /// <summary>Get all the number of entries in a column</summary>
        /// <param name="column_id">the column ID</param>
        /// <returns>the number of entries in column column_id</returns>
        //int NumEntriesByColumn(int column_id);

        /// <summary>Get the overlap of two matrices, i.e. the number of true entries where they agree</summary>
        /// <param name="s">the <see cref="IBooleanMatrix"/> to compare to</param>
        /// <returns>the number of entries that are true in both matrices</returns>
        //int Overlap(IBooleanMatrix s);
    }
}
