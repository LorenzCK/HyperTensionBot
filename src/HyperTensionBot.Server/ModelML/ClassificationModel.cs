using Microsoft.ML;
using System.IO;

namespace HyperTensionBot.Server.ModelML {
    // model for classification a user message
    public class ClassificationModel {

        private readonly MLContext mlContext;
        private ITransformer model;
        private ModelTrainer trainer;
        private string pathFile;
        private string pathModel;

        public ClassificationModel(WebApplicationBuilder builder) {
            mlContext = new MLContext();
            trainer = new ModelTrainer(mlContext);
            if (ConfigurePath(builder)) {
                trainer.Train(pathFile, pathModel);
                model = mlContext.Model.Load(Path.Combine(pathModel, "model.zip"), out var modelInputSchema);
            }
        }

        private bool ConfigurePath(WebApplicationBuilder builder) {

            var confModel = builder.Configuration.GetSection("ClassificationModel");
            if (!confModel.Exists() || string.IsNullOrEmpty(confModel["trainingData"]) || string.IsNullOrEmpty(confModel["model"]))
                return false;
            else {
                this.pathFile = confModel["trainingData"] ?? throw new ArgumentException("Configuration model: path training set is not set"); 
                this.pathModel = confModel["model"] ?? throw new ArgumentException("Configuration model: path model is not set");
                // delete old folder and create new
                if (Directory.Exists(pathModel)) {
                    Directory.Delete(pathModel, true);
                }
                Directory.CreateDirectory(pathModel);
                return true;
            }

        }

        // method for predict
        public Intent Predict(ModelInput input) {
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);
            return (Intent)Enum.Parse(typeof(Intent), predictionEngine.Predict(input).PredictedLabel);
        }
    }
}
