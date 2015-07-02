using MyMediaLite.DataType;
using MyMediaLite.ItemRecommendation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recommender
{
    class CustomBPRMF : BPRMF
    {
        public float GetNearestItemSimilarity(int user_id, int item_id)
        {
            float minSimilarity = 1000.0F;
            //IItemFeaturesMatrix featureMatrix = Recommender.feedback.UserMatrix;

            IList<int> itemsOfUser = Feedback.UserMatrix.GetEntriesByRow(user_id);
            foreach (int itemBought in itemsOfUser)
            {
                float similarityForItem = GetItemSimilarity(itemBought, item_id);
                if (similarityForItem < minSimilarity)
                {
                    minSimilarity = similarityForItem;
                }
            }

            return minSimilarity;
        }

        public float GetNearestUserSimilarity(int user_id, int item_id)
        {
            float minSimilarity = 1000.0F;
            //IItemFeaturesMatrix featureMatrix = Recommender.feedback.UserMatrix;

            IList<int> usersOfItem = Feedback.ItemMatrix.GetEntriesByRow(item_id);
            foreach (int user in usersOfItem)
            {
                float similarityForUser = GetUserSimilarity(user, user_id);
                if (similarityForUser < minSimilarity)
                {
                    minSimilarity = similarityForUser;
                }
            }

            return minSimilarity;
        }

        private float GetUserSimilarity(int user, int newUser)
        {
            IList<float> rowDiff = MatrixExtensions.RowDifference(user_factors, user, user_factors, newUser);
            return (float)rowDiff.EuclideanNorm();
        }

        private float GetItemSimilarity(int itemBought, int newItem)
        {
            IList<float> rowDiff = MatrixExtensions.RowDifference(item_factors, itemBought, item_factors, newItem);
            return (float) rowDiff.EuclideanNorm();
        }
    }
}
