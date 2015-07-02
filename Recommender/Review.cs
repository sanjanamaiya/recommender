using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Recommender
{
    class Review
    {
        public string itemID { get; set; }
        public double rating { get; set; }
        public RatingDetails helpful { get; set; }
        public List<List<string>> category { get; set; }
        public int unixReviewTime { get; set; }
        public string reviewText { get; set; }
        public string reviewerID { get; set; }
        public string reviewTime { get; set; }
        public string summary { get; set; }
    }
}
