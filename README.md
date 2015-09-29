# recommender

A recommender system which predicts ratings of Amazon products, and predicts if a user purchased an item or not.
The training and test data is not uploaded. It consists of around 900,000 Amazon reviews in json format.

The executable is compiled with .NET 4.5, and runs on Windows machine with .NET 4.5 runtime, and on Linux devices with Mono (not tested on Linux though)
The executable is within the zipped folder Release_executable.zip

To predict ratings, run
Recommender.exe predictRatings <location_of_input_files> <location_of_output_files>
For example, 
Recommender.exe predictRatings "D:\\downloads\\info_retrieval\\homework2\\homework2-data\\task1-rating" "D:\\downloads\\info_retrieval\\homework2\\homework2-data\\output"
Here, the folder task1-rating has the files test_rating_label.txt, train_rating.json and test_rating.txt
The output file output_ratings.txt will be created with the predicted ratings for the test data
The RMSE will be displayed on the screen

To predict items for users, run
Recommender.exe predictItems <location_of_input_files> <location_of_output_files>
For example
Recommender.exe predictItems "D:\\downloads\\info_retrieval\\homework2\\homework2-data\\task23-purchase" "D:\\downloads\\info_retrieval\\homework2\\homework2-data\\output"
Here, the folder task23-purchase has the files test_purchase_label.txt, train_purchase.json and test_purchase.txt
The output file output_item_prediction.txt will be created with the predicted ratings for the test data
The output folder will also contain the trained model which which is created during the first run. All subsequent runs use this model for prediction
