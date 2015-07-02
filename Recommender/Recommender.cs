using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyMediaLite.Data;
using MyMediaLite.Eval;
using MyMediaLite.IO;
using MyMediaLite.RatingPrediction;
using MyMediaLite.ItemRecommendation;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;

namespace Recommender
{
    class Recommender
    {

        public static FeatureFeedBack<ItemFeaturesMatrix> feedback = null;
        private static List<String> tempReviewData = new List<String>();
        private static String trainingCompactFileForItems = "training_modified_items.txt";
        private static String trainingCompactFileForRatings = "training_modified_ratings.txt";
        private static String outputFileForRatings = "output_ratings.txt";
        private static String outputFileForItems = "output_item_prediction.txt";
        private static String testingFileModified = "test_rating_modified.txt";
        private static String testingFile = "test_rating_label.txt";
        private static String testingFileForItems = "test_purchase_label.txt";
        private static Dictionary<string, List<List<string>>> itemDetailsInfo = new Dictionary<string, List<List<string>>>();
        public static int totalActualPurchases = 0;
        public static int truePositives = 0;
        public static int falsePositives = 0;
        public static int falseNegatives = 0;
        public static int trueNegatives = 0;
        public static SortedDictionary<float, List<MeanAverPrecisionDetails>> rankedPurchase;
        public static float averagePrecisionSum = 0.0F;
        public static Dictionary<string, List<float>> averageItemRatings = new Dictionary<string, List<float>>();
        public static int mostRecentReview = 0;

        public const String predictRatingsString = "predictRatings";
        public const String predictItemsString = "predictItems";
        public const String trainingFileForRating = "train_rating.json";
        public const String trainingFileForItem = "train_purchase.json";

        static void Main(string[] args)
        {
            Stopwatch timeKeeper = new Stopwatch();
            timeKeeper.Start();

            if (args.Length < 3)
            {
                DisplayHelp();
                Console.ReadLine();
                return;
            }

            if (predictRatingsString.Equals(args[0], StringComparison.OrdinalIgnoreCase))
            {
                ParseTrainingFileForRatings(args);
                PredictRating(args);
            }
            else
            {
                ParseTrainingFileForItems(args);
            }
            
            timeKeeper.Stop();
            Console.WriteLine("time passed for program: " + timeKeeper.ElapsedMilliseconds);
            Console.ReadLine();
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("Recommender Usage:\n" +
                "Recommender.exe <predictRatings | predictItems> <trainingAndTestFileLocation> <outputDirectoryLocation> \n\n" +
                "For example: \n" +
                "Recommender.exe predictRating C:\\testFiles C:\\output\n" +
                "The files required in testFiles folder include train_rating.json, test_rating.txt, and test_rating_label.txt\n");
            Console.WriteLine("Press Any key to Exit");
        }

        private static void ParseTrainingFileForRatings(string[] args)
        {
            trainingCompactFileForRatings = Path.Combine(args[2], trainingCompactFileForRatings);
            if (File.Exists(trainingCompactFileForRatings))
            {
                // do nothing
                return;
            }

            StreamReader reader = null;
            try
            {
                reader = new StreamReader(Path.Combine(args[1], trainingFileForRating));//(args[0]);
                String line = null;
                Review trainingReview = null;
                if (reader != null)
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        trainingReview = JsonConvert.DeserializeObject<Review>(line);
                        WriteInfoToFile(trainingCompactFileForRatings, trainingReview.reviewerID, trainingReview.itemID, trainingReview.rating, trainingReview.unixReviewTime, true);
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("exception reading the training file : {0}", e.Message);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }
        }

        public static Mapping userMappingForItemPrediction = new Mapping();
        public static Mapping itemMappingForItemPrediction = new Mapping();
        private static void ParseTrainingFileForItems(string[] args)
        {
            StreamReader reader = null;
            try
            {
                trainingCompactFileForItems = Path.Combine(args[2], trainingCompactFileForItems);
                if (File.Exists(trainingCompactFileForItems))
                {
                    File.Delete(trainingCompactFileForItems);
                }
                reader = new StreamReader(Path.Combine(args[1], trainingFileForItem));//(args[0]);
                String line = null;
                Review trainingReview = null;

                feedback = new FeatureFeedBack<ItemFeaturesMatrix>();
                if (reader != null)
                {
                    while ((line = reader.ReadLine()) != null)
                    {
                        trainingReview = JsonConvert.DeserializeObject<Review>(line);
                        WriteInfoToFile(trainingCompactFileForItems, trainingReview.reviewerID, trainingReview.itemID, trainingReview.rating, trainingReview.unixReviewTime, false);                        
                        int userId = userMappingForItemPrediction.ToInternalID(trainingReview.reviewerID);
                        int itemId = itemMappingForItemPrediction.ToInternalID(trainingReview.itemID);
                        feedback.Add(userId, itemId, new UserItemData(trainingReview.unixReviewTime, (float)trainingReview.rating, trainingReview.category, trainingReview.helpful.nHelpful / trainingReview.helpful.outOf));
                        if (!itemDetailsInfo.ContainsKey(trainingReview.itemID))
                            itemDetailsInfo.Add(trainingReview.itemID, trainingReview.category);

                        if (!averageItemRatings.ContainsKey(trainingReview.itemID))
                        {
                            List<float> ratingAndNumOfRating = new List<float>();
                            ratingAndNumOfRating.Add((float)trainingReview.rating);
                            ratingAndNumOfRating.Add(1.0F);
                            averageItemRatings.Add(trainingReview.itemID, ratingAndNumOfRating);
                        }
                        else
                        {
                            List<float> existingRating = averageItemRatings[trainingReview.itemID];
                            existingRating[1]++;
                            existingRating[0] += (float)trainingReview.rating;
                            averageItemRatings[trainingReview.itemID] = existingRating;
                        }
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("exception reading the training file : {0}", e.Message);
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
            }

            Stopwatch timeKeeper = new Stopwatch();
            timeKeeper.Start();
            TrainForItemPrediction(userMappingForItemPrediction, itemMappingForItemPrediction, args);
            timeKeeper.Stop();
            Console.WriteLine("time passed for training item prediction model: " + timeKeeper.ElapsedMilliseconds);
            timeKeeper = new Stopwatch();
            timeKeeper.Start();
            RecommentItems(userMappingForItemPrediction, itemMappingForItemPrediction, args);
            timeKeeper.Stop();
            Console.WriteLine("time passed for item prediction: " + timeKeeper.ElapsedMilliseconds);
        }

        private static CustomBPRMF itemRecommender;
        private static void TrainForItemPrediction(Mapping userMapping, Mapping itemMapping, String[] args)
        {
            var training_data = ItemData.Read(trainingCompactFileForItems, userMapping, itemMapping);
            itemRecommender = new CustomBPRMF();
            if (File.Exists(Path.Combine(args[2], "model")))
            {
                Console.WriteLine("Skipping training, Loading saved model");
                itemRecommender.LoadModel(Path.Combine(args[2], "model"));
                itemRecommender.Feedback = training_data;
                return;
            }

            Console.WriteLine("Training model for Item Prediction, this may take a while...");
            itemRecommender.Feedback = training_data;
            itemRecommender.NumFactors = 50;
            itemRecommender.NumIter = 100;
            itemRecommender.Train();
            itemRecommender.SaveModel(Path.Combine(args[2], "model"));
        }

        private static void WriteInfoToFile(string fileName, string userId, string itemId, double rating, int time, bool isRatingPrediction)
        {
            if (tempReviewData.Count < 50000)
            {
                string ratingString = isRatingPrediction?  "," + String.Format("{0:N1}", rating) : "";
                tempReviewData.Add(userId + "," + itemId + ratingString);
            }
            else
            {
                // flush to file
                StreamWriter writer = null;
                try
                {
                    writer = new StreamWriter(fileName, true);
                    if (writer != null)
                    {
                        foreach (string singleReview in tempReviewData)
                        {
                            writer.WriteLine(singleReview);
                        }
                    }
                }
                catch (Exception)
                {
                    // TODO
                    // data may get messed up, will lose a max of 50000 reviews
                }
                finally
                {
                    // done, reset the tempDataList
                    tempReviewData.Clear();

                    if (writer != null)
                    {
                        writer.Close();
                    }
                }
            }
            //Console.WriteLine("UserId : {0}, ItemId : {1}, Rating: {2} ", userId, itemId, rating); 
        }

        /// <summary>
        /// Predict the rating of the item by users
        /// </summary>
        private static void PredictRating(string[] args)
        {
            Console.WriteLine("Predicting ratings for Users...");
            String outputFile = Path.Combine(args[2], outputFileForRatings);
            testingFile = Path.Combine(args[1], testingFile);
            testingFileModified = Path.Combine(args[2], testingFileModified);
            ModifyTestingFileForRating(testingFile, testingFileModified);

            float minRating = 1;
            float maxRating = 5;

            var userMapping = new Mapping();
            var itemMapping = new Mapping();
            var trainingData = StaticRatingData.Read(trainingCompactFileForRatings, userMapping, itemMapping, RatingType.FLOAT, TestRatingFileFormat.WITH_RATINGS, false);
            var testUsers = trainingData.AllUsers; // users that will be taken into account in the evaluation
            var candidate_items = trainingData.AllItems; // items that will be taken into account in the evaluation
            var testData = StaticRatingData.Read(testingFileModified, userMapping, itemMapping, RatingType.FLOAT, TestRatingFileFormat.WITH_RATINGS, false);

            var recommender = new BiasedMatrixFactorization();
            recommender.MinRating = minRating;
            recommender.MaxRating = maxRating;
            recommender.Ratings = trainingData;
            
            recommender.NumFactors = 30;
            recommender.NumIter = 100;
            recommender.RegI = 0.04F;
            recommender.RegU = 0.04F;
            //recommender.BiasReg = 0.09F;
            recommender.FrequencyRegularization = true;
            recommender.BoldDriver = true;
            recommender.LearnRate = 0.07F;
            
            Stopwatch timeKeeper = new Stopwatch();
            timeKeeper.Start();
            recommender.Train();
            timeKeeper.Stop();
            Console.WriteLine("time passed for training rating prediction model: " + timeKeeper.ElapsedMilliseconds);
            // measure the accuracy on the test data set

            timeKeeper = new Stopwatch();
            timeKeeper.Start();
            var results = recommender.Evaluate(testData);
            timeKeeper.Stop();
            Console.WriteLine("time passed for rating prediction: " + timeKeeper.ElapsedMilliseconds);
            Console.WriteLine("RMSE={0}", results["RMSE"]);

            recommender.WritePredictions(testData, outputFile, userMapping, itemMapping, "{0}-{1},{2}", "userID-itemID,rating");
        }

        private static void ModifyTestingFileForRating(string testingFile, string testingFileModified)
        {
            if (File.Exists(testingFileModified))
            {
                return;
            }
            StreamReader testFileReader = null;
            StreamWriter testFileWriter = null;

            try
            {
                testFileReader = new StreamReader(testingFile);
                testFileWriter = new StreamWriter(testingFileModified, true);
                String line = null;
                testFileReader.ReadLine();  // Don't need first line
                if (testFileReader != null)
                {
                    while ((line = testFileReader.ReadLine()) != null)
                    {
                        String[] user_item_rating = line.Split(',');
                        String[] user_item = user_item_rating[0].Split('-');
                        testFileWriter.WriteLine(user_item[0] + "," + user_item[1] + "," + user_item_rating[1]);
                    }
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                if (testFileReader != null)
                {
                    testFileReader.Close();
                }
                if (testFileWriter != null)
                {
                    testFileWriter.Close();
                }
            }
        }

        public static void RecommentItems(Mapping userMapping, Mapping itemMapping, String[] args)
        {
            Console.WriteLine("Predicting Items for Users...");
            testingFileForItems = Path.Combine(args[1], testingFileForItems);
            if (File.Exists(testingFileForItems))
            {
                StreamReader reader = null;
                try
                {
                    reader = new StreamReader(testingFileForItems);//(args[0]);
                    String line = null;
                    int userCount = 0;
                    if (reader != null)
                    {
                        line = reader.ReadLine();
                        line = reader.ReadLine();
                        while (line != null)
                        {
                            String[] labels = line.Split(',');
                            String[] user_item = labels[0].Split('-');
                            string user = user_item[0];
                            Dictionary<string, List<string>> itemsPurchase = new Dictionary<string, List<string>>();
                            List<string> itemPurchaseDetails = new List<string>();
                            itemPurchaseDetails.Add(labels[1]); // bought or not, 0 or 1
                            itemPurchaseDetails.Add(labels[2]); // rank of this item 
                            itemsPurchase.Add(user_item[1], itemPurchaseDetails);
                            line = reader.ReadLine();
                            while (line != null && user.Equals(line.Split(',')[0].Split('-')[0]))
                            {
                                labels = line.Split(',');
                                user_item = labels[0].Split('-');
                                if (!user.Equals(user_item[0]))
                                    break;

                                itemPurchaseDetails = new List<string>();
                                itemPurchaseDetails.Add(labels[1]); // bought or not, 0 or 1
                                itemPurchaseDetails.Add(labels[2]);
                                itemsPurchase.Add(user_item[1], itemPurchaseDetails);
                                line = reader.ReadLine();
                            }

                            PredictPurchase(user, itemsPurchase, userMapping, itemMapping, args);
                            userCount++;
                        }
                    }

                    double precision = (double) truePositives / (truePositives + falsePositives);
                    double recall = (double)truePositives / (truePositives + falseNegatives);//(totalActualPurchases);
                    double f1Measure = 2 * ((precision * recall) / (precision + recall));
                    double meanAveragePrecision = averagePrecisionSum / userCount;
                    Console.WriteLine("precision : {0}, recall : {1}, f1 : {2}, MAP : {3} ", precision, recall, f1Measure, meanAveragePrecision);
                }
                catch (IOException e)
                {
                    Console.WriteLine("exception reading the training file : {0}", e.Message);
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                }
            }
        }

        
        private static void PredictPurchase(string user, Dictionary<string, List<string>> itemsPurchases, Mapping userMapping, Mapping itemMapping, String[] args)
        {
            // Get the master set - feedback
            string logFile = Path.Combine(args[2], "ItemPrediction.log");
            StreamWriter writer = null;
            try
            {
                writer = new StreamWriter(logFile, true);
                float result1 = 0.0F;
                //Stopwatch timeKeeper = new Stopwatch();
                //timeKeeper.Start();
                rankedPurchase = new SortedDictionary<float, List<MeanAverPrecisionDetails>>();//Comparer<float>.Create((x, y) => y.CompareTo(x)));
            
                // this user's average ratings - does he buy items which are low rated?
                float[] userAverageAndNumberOfRatings = GetAverageRatingAndNumberOfRatingsGivenByUser(userMapping.ToInternalID(user));
                float averageRatingByUser = userAverageAndNumberOfRatings[0];
                float numberOfRatingsByUser = userAverageAndNumberOfRatings[1];
                //bool isUserActive = UserActivity(userMapping.ToInternalID(user));
                Dictionary<string, int> userCategories = GetUserPurchaseCategories(userMapping.ToInternalID(user));
                foreach (String item in itemsPurchases.Keys)
                {
                    float resultForItemSimilarity = itemRecommender.GetNearestItemSimilarity(userMapping.ToInternalID(user), itemMapping.ToInternalID(item));
                    //float resultForUserSimilarity = itemRecommender.GetNearestUserSimilarity(userMapping.ToInternalID(user), itemMapping.ToInternalID(item));
                    float averageRatingForItem = GetAverageRatingForItem(item);
                    bool predictedValueOfPurchase = false;

                    float resultFromItemCategories = CompareUserItemCategories(userCategories, GetItemCategories(item));
                    
                    float result = resultForItemSimilarity;
                    //if (averageRatingForItem > userAverageRating)
                    //    result += ((averageRatingForItem - userAverageRating)/ averageRatingForItem);

                    if (averageRatingForItem > 4.0)
                    {
                        resultFromItemCategories = +((averageRatingForItem - 4.0F) / averageRatingForItem);
                    }

                    ////result += userActivity;
                    //if (!isUserActive)
                    //{
                    //    result -= 0.2f;//(0.1F * result);
                    //}

                    if (resultFromItemCategories > 0.45)
                    {
                        result = result - (1.0F * resultFromItemCategories);
                    }

                    //if (numberOfRatingsByUser > 10)
                    //{
                    //    result = result - 0.1F;
                    //}
                    writer.WriteLine("User: {0}, Item: {1}, hasBought: {2}, result: {3}, rating for Item {4}", user, item, itemsPurchases[item][0], result, averageRatingForItem);
                    result1 +=result;
                    if (result < 0.4)
                    {
                        predictedValueOfPurchase = true;
                    }
                    List<MeanAverPrecisionDetails> elementsToAddToRankedPurchaseList = new List<MeanAverPrecisionDetails>();
                    elementsToAddToRankedPurchaseList.Add(new MeanAverPrecisionDetails(user, item, predictedValueOfPurchase ? 1 : 0, itemsPurchases[item][0].Equals("1") ? 1 : 0, result, Int32.Parse(itemsPurchases[item][1])));
                
                    if (!rankedPurchase.ContainsKey(result))
                    {
                        rankedPurchase.Add(result, elementsToAddToRankedPurchaseList);
                    }
                    else
                    {
                        List<MeanAverPrecisionDetails> exisitingValues = rankedPurchase[result];
                        exisitingValues.AddRange(elementsToAddToRankedPurchaseList);
                        rankedPurchase[result] = exisitingValues;
                    }

                
                    // User has actually bought the item
                    if (itemsPurchases[item][0].Equals("1"))
                    {
                        totalActualPurchases++;
                        if (predictedValueOfPurchase)
                        {
                            // We have correctly identified that the user has purchased item
                            truePositives++;
                        }
                        else
                        {
                            // Missed predicting the user's purchase
                            falseNegatives++;
                        }
                    }
                    else
                    {
                        // User has actually not bought this item
                        if (predictedValueOfPurchase)
                        {
                            // Wrongly predicted that the user bought the item
                            falsePositives++;
                        }
                        else
                        {
                            // Correctly predicted that the user did not buy item
                            trueNegatives++;
                        }
                    }
                
                    // Find out if this user has bought items from the same category(ies)
                }


                CalculateAveragePrecision(args);
                writer.Close();
                //timeKeeper.Stop();
                //Console.WriteLine("time passed PredictPurchase: " + timeKeeper.ElapsedMilliseconds);
            }
            catch (Exception) {}
            finally
            {
                if (writer != null)
                {
                    writer.Close();
                }
            }
        }

        private static void CalculateAveragePrecision(String[] args)
        {
            float precisionAtRankkSums = 0.0F;
            float precisionAtRankkSums2 = 0.0F;
            rankedPurchase.Reverse();
            int rank = 1;
            int numberOfPurchasesPredictedCorrectly  = 0;
            int numberOfActualPurchases = 0;
            int numberOfFalsePositives = 0;
            outputFileForItems = Path.Combine(args[2], outputFileForItems);
            string logFileItemPred = Path.Combine(args[2], "test_ranks.log");
            StreamWriter writer = null;
            StreamWriter writer2 = null;

            try
            {
                writer = new StreamWriter(outputFileForItems, true);
                writer2 = new StreamWriter(logFileItemPred, true);
                foreach (float rankingValue in rankedPurchase.Keys)
                {
                    List<MeanAverPrecisionDetails> rankingDetails = rankedPurchase[rankingValue];
                    foreach (MeanAverPrecisionDetails details in rankingDetails)
                    {
                        writer.WriteLine("{0}-{1}, {2}, {3}", details.UserId, details.ItemId, details.PredictedPurchase, rank);
                        writer2.WriteLine("{0}-{1}, {2}, {3}, {4}, {5}, {6}", details.UserId, details.ItemId, details.PredictedPurchase, rank, details.Rating, details.ActualValueOfPurchase, GetAverageRatingForItem(details.ItemId));
                        if (details.ActualValueOfPurchase == 1)
                        {
                            numberOfActualPurchases++;
                            precisionAtRankkSums2 += (float)(details.ActualRank) / rank;
                            if (details.PredictedPurchase == 1)
                            {
                                numberOfPurchasesPredictedCorrectly++;
                            }
                        }
                        else
                        {
                            if (details.PredictedPurchase == 1)
                            {
                                numberOfFalsePositives++;
                            }
                        }

                        // precision @ rank k calculations ...
                        int totalNumberOfPurchasesPredicted = (numberOfPurchasesPredictedCorrectly + numberOfFalsePositives);
                        totalNumberOfPurchasesPredicted = (totalNumberOfPurchasesPredicted == 0) ? 1 : totalNumberOfPurchasesPredicted;
                        precisionAtRankkSums += (float)(numberOfPurchasesPredictedCorrectly / totalNumberOfPurchasesPredicted) * details.ActualValueOfPurchase;
                        rank++;
                    }
                }

                //averagePrecisionSum += (precisionAtRankkSums / numberOfActualPurchases);
                averagePrecisionSum += (precisionAtRankkSums2 / numberOfActualPurchases);
            }
            catch (Exception) { }
            finally
            {
                if (writer != null)
                    writer.Close();
                if (writer2 != null)
                    writer2.Close();
            }
        }

        private static bool UserActivity(int userId)
        {
            IItemFeaturesMatrix featureMatrix = feedback.UserMatrix;
            IDictionary<int, UserItemData> itemEntriesForUser = featureMatrix.GetFeatureEntriesByRow(userId);
            bool active = true;
            int numberOfRatings = itemEntriesForUser.Count;
            int mostRecentPurchaseTime = 0;
            int numberOfRecentPurchases = 0;
            int totalPurchases = 0;
            foreach (UserItemData ratingData in itemEntriesForUser.Values)
            {
                if (ratingData.ReviewTime > mostRecentPurchaseTime)
                {
                    mostRecentPurchaseTime = ratingData.ReviewTime;
                }

                if ((mostRecentReview - ratingData.ReviewTime) < 7776000)
                {
                    numberOfRecentPurchases++;
                }
                totalPurchases++;
            }

            // Has reviewed an item in the last 3 months
            if ((mostRecentReview - mostRecentPurchaseTime) > 31536000)
            {
                active = false;
            }

            return active;
            //return (float)numberOfRecentPurchases / totalPurchases;
            
        }

        private static HashSet<string> GetItemCategories(string item)
        {
            HashSet<string> categories = new HashSet<string>();
            foreach (List<string> itemCategories in itemDetailsInfo[item])
            {
                foreach (string cat in itemCategories)
                {
                    categories.Add(cat);
                }
            }
            return categories;
        }

        private static float CompareUserItemCategories(Dictionary<string, int> userCategories, HashSet<string> list)
        {
            // This item has categories x, y, z
            // Check if the user usually buys from these categories
            Stopwatch timeKeeper = new Stopwatch();
            timeKeeper.Start();
            float totalScore = 0;
            
            foreach (string cat in list)
            {
                if (userCategories.ContainsKey(cat))
                {
                    totalScore += userCategories[cat];
                }
            }

            timeKeeper.Stop();
            //Console.WriteLine("time passed CompareUserItemCategories: " + timeKeeper.ElapsedMilliseconds);
            return (float)totalScore/totalCategyItems;
        }


        private static List<string> GetItemCategories(int itemId)
        {
            Stopwatch timeKeeper = new Stopwatch();
            timeKeeper.Start();
            IItemFeaturesMatrix featureMatrix = feedback.UserMatrix;
            IDictionary<int, UserItemData> userEntriesForItem = featureMatrix.GetFeatureEntriesByColumn(itemId);
            foreach (UserItemData ratingData in userEntriesForItem.Values)
            {
                timeKeeper.Stop();
                Console.WriteLine("time passed GetItemCategories: " + timeKeeper.ElapsedMilliseconds);
                return ratingData.Category[0];
            }
            return null;
        }

        public static int  totalCategyItems = 0;
        private static Dictionary<string, int> GetUserPurchaseCategories(int user)
        {
            
            totalCategyItems = 0;
            IItemFeaturesMatrix featureMatrix = feedback.UserMatrix;
            Dictionary<string, int> categories = new Dictionary<string, int>();
            IDictionary<int, UserItemData> itemEntriesForUser = featureMatrix.GetFeatureEntriesByRow(user);
            foreach (UserItemData ratingData in itemEntriesForUser.Values)
            {
                List<List<string>> itemCategoriesList = ratingData.Category;
                foreach (List<string> itemCategories in itemCategoriesList)
                {
                    foreach (string cat in itemCategories)
                    {
                        if (!categories.ContainsKey(cat))
                        {
                            categories.Add(cat, 1);
                        }
                        else
                        {
                            categories[cat]++;
                        }

                    }
                }
            }
            foreach (int count in categories.Values)
            {
                totalCategyItems += count;
            }
            
            return categories;
        }

        private static float[] GetAverageRatingAndNumberOfRatingsGivenByUser(int user)
        {
            float[] averageAndNumberOfRatings = new float[2];
            IItemFeaturesMatrix featureMatrix = feedback.UserMatrix;
            IDictionary<int, UserItemData> itemEntriesForUser = featureMatrix.GetFeatureEntriesByRow(user);
            float ratingsSum = 0.0F;
            int numberOfRatings = itemEntriesForUser.Count;
            foreach (UserItemData ratingData in itemEntriesForUser.Values)
            {
                ratingsSum += ratingData.Rating;
            }
            averageAndNumberOfRatings[0] = (ratingsSum / ((numberOfRatings == 0) ? 1 : numberOfRatings));
            averageAndNumberOfRatings[1] = (float) numberOfRatings;

            return averageAndNumberOfRatings;
        }

        private static float GetAverageRatingForItem(string itemId)
        {
            float averageRating = 0.0F;
            if (averageItemRatings.ContainsKey(itemId))
            {
                List<float> ratingDetails = averageItemRatings[itemId];
                averageRating = ratingDetails[0] / ratingDetails[1];
            }
            //Stopwatch timeKeeper = new Stopwatch();
            //timeKeeper.Start();
            //IItemFeaturesMatrix featureMatrix = feedback.UserMatrix;
            //IDictionary<int, UserItemData> userEntriesForItem = featureMatrix.GetFeatureEntriesByColumn(itemId);
            //float ratingsSum = 0.0F;
            //int numberOfRatings = userEntriesForItem.Count;
            //foreach (UserItemData ratingData in userEntriesForItem.Values)
            //{
            //    ratingsSum += ratingData.Rating;
            //}

            //timeKeeper.Stop();
            //Console.WriteLine("time passed GetAverageRatingForItem: " + timeKeeper.ElapsedMilliseconds);
            //float averageRating = (ratingsSum / ((numberOfRatings == 0) ? 1 : numberOfRatings));
            //averageItemRatings.Add(itemId, averageRating);
            return averageRating;
        }
    }

    class MeanAverPrecisionDetails
    {
        public MeanAverPrecisionDetails(string userId, string itemId, int predictedPurchase, int actualPurchase, float rating, int actualRank)
        {
            this.userId = userId;
            this.itemId = itemId;
            this.predictedPurchase = predictedPurchase;
            this.actualValueOfPurchase = actualPurchase;
            this.rating = rating;
            this.actualRank = actualRank;
        }
        private string userId;

        public string UserId
        {
            get { return userId; }
            set { userId = value; }
        }
        private string itemId;

        public string ItemId
        {
            get { return itemId; }
            set { itemId = value; }
        }
        private int predictedPurchase;

        public int PredictedPurchase
        {
            get { return predictedPurchase; }
            set { predictedPurchase = value; }
        }
        private float rating;

        public float Rating
        {
            get { return rating; }
            set { rating = value; }
        }
        private int actualValueOfPurchase;

        public int ActualValueOfPurchase
        {
            get { return actualValueOfPurchase; }
            set { actualValueOfPurchase = value; }
        }
        private int actualRank;

        public int ActualRank
        {
            get { return actualRank; }
            set { actualRank = value; }
        }


    }
}
