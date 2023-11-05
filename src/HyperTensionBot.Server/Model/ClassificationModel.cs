using Microsoft.ML;

namespace HyperTensionBot.Server.Model {
    // model for classification a user message
    public class ClassificationModel {

        private readonly MLContext mlContext;
        private ITransformer model;

        public ClassificationModel(string modelPath) {
            mlContext = new MLContext();
            model = mlContext.Model.Load(modelPath, out var modelInputSchema);
        }

        // method for predict
        public Intent Predict(ModelInput input) {
            var predictionEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(model);
            return (Intent)Enum.Parse(typeof(Intent), predictionEngine.Predict(input).PredictedLabel);
        }
    }
}
