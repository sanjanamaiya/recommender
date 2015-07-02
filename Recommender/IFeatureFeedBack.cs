using MyMediaLite.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recommender
{
    interface IFeatureFeedBack : IDataSet
    {
        /// <summary>By-user access, users are stored in the rows, items in the culumns</summary>
        /// <remarks>should be implemented as lazy data structure</remarks>
        IItemFeaturesMatrix UserMatrix { get; }

        /// <summary>By-item access, items are stored in the rows, users in the culumns</summary>
        /// <remarks>should be implemented as lazy data structure</remarks>
        IItemFeaturesMatrix ItemMatrix { get; }

        /// <summary>Add a user-item event to the data structure</summary>
        /// <param name="user_id">the user ID</param>
        /// <param name="item_id">the item ID</param>
        void Add(int user_id, int item_id, UserItemData details);

        /// <summary>Get a copy of the item matrix</summary>
        /// <returns>a copy of the item matrix</returns>
        IItemFeaturesMatrix GetItemMatrixCopy();

        /// <summary>Get a copy of the user matrix</summary>
        /// <returns>a copy of the user matrix</returns>
        IItemFeaturesMatrix GetUserMatrixCopy();

        /// <summary>Remove a user-item event from the data structure</summary>
        /// <remarks>
        /// If no event for the given user-item combination exists, nothing happens.
        /// </remarks>
        /// <param name="user_id">the user ID</param>
        /// <param name="item_id">the item ID</param>
        void Remove(int user_id, int item_id);

        /// <summary>Get the transpose of the dataset (users and items exchanged)</summary>
        /// <returns>the transpose of the dataset</returns>
        IPosOnlyFeedback Transpose();
    }
}
