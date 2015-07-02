using MyMediaLite.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Recommender
{
    class FeatureFeedBack<T> : DataSet, IFeatureFeedBack, ISerializable where T : IItemFeaturesMatrix, new()
    {
        public override void RemoveItem(int item_id)
        {
            throw new NotImplementedException();
        }

        public override void RemoveUser(int user_id)
        {
            throw new NotImplementedException();
        }

        IItemFeaturesMatrix user_matrix;

        public IItemFeaturesMatrix UserMatrix
        {
            get
            {
                if (user_matrix == null)
                    user_matrix = new ItemFeaturesMatrix();

                return user_matrix;
            }
        }
       

        public IItemFeaturesMatrix ItemMatrix
        {
            get { throw new NotImplementedException(); }
        }

        public void Add(int user_id, int item_id, UserItemData details)
        {
            Users.Add(user_id);
            Items.Add(item_id);

            if (UserMatrix != null)
                UserMatrix[user_id, item_id] = details;

            if (user_id > MaxUserID)
                MaxUserID = user_id;

            if (item_id > MaxItemID)
                MaxItemID = item_id;
        }

        public IItemFeaturesMatrix GetItemMatrixCopy()
        {
            throw new NotImplementedException();
        }

        public IItemFeaturesMatrix GetUserMatrixCopy()
        {
            var matrix = new T();
            for (int index = 0; index < Count; index++)
                matrix[Users[index], Items[index]] = null;
            return matrix;
        }

        public void Remove(int user_id, int item_id)
        {
            int index = -1;

            while (TryGetIndex(user_id, item_id, out index))
            {
                Users.RemoveAt(index);
                Items.RemoveAt(index);
            }

            if (user_matrix != null)
                user_matrix[user_id, item_id] = null;
        }

        public IPosOnlyFeedback Transpose()
        {
            throw new NotImplementedException();
        }
    }
}
