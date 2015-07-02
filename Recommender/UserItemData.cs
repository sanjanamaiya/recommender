using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recommender
{
    class UserItemData
    {
        public UserItemData(int reviewerTime, float rating, List<List<string>> category, float helpful)
        {
            ReviewTime = reviewerTime;
            Rating = rating;
            Category = category;
            Helpful = helpful;
        }

        private int reviewTime;
        public int ReviewTime
        {
            get { return reviewTime; }
            set { reviewTime = value; }
        }
        
        private float rating;
        public float Rating
        {
            get { return rating; }
            set { rating = value; }
        }
        
        private List<List<string>> category;
        public List<List<string>> Category
        {
            get { return category; }
            set { category = value; }
        }

        private float helpful;
        public float Helpful
        {
            get { return helpful; }
            set { helpful = value; }
        }
    }
}
