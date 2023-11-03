using Microsoft.ML.Data;

namespace HyperTensionBot.Server.Model {
    public class ModelInput {

        [LoadColumn(0)]
        [ColumnName(@"Sentence")]
        public string Sentence { get; set; }

        [LoadColumn(1)]
        [ColumnName(@"Label")]
        public float Label { get; set; }
    }
}
