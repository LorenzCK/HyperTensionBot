using HyperTensionBot.Server.ModelML;

namespace HyperTensionBot.Server.Bot {
    public static class Context {

        public static void controlFlow(Intent context) {
            switch (context) {
                case Intent.richiestaStatsGenerTotale:
                case Intent.richiestaStatsGenerMens:
                case Intent.richiestaStatsGenerSett:
                case Intent.richiestaStatsGenerRece:
                case Intent.richiestaStatsPressTot:
                case Intent.richiestaStatsPressMens:
                case Intent.richiestaStatsPressSett:
                case Intent.richiestaStatsPressRece:
                case Intent.richiestaStatsFreqTot:
                case Intent.richiestaStatsFreqMens:
                case Intent.richiestaStatsFreqSett:
                case Intent.richiestaStatsFreqRece:
                case Intent.inserDatiGener:
                case Intent.inserDatiPress:
                case Intent.inserDatiFreq:
                case Intent.inserDatiTot:
                case Intent.pazienAllarmato:
                case Intent.pazienteSereno:
                case Intent.saluti:
                case Intent.fuoriCont:
                case Intent.spiegazioni:
                case Intent.fuoriContMed:
                case Intent.richiestaInsDati:
                    break;

            }
        }

        // elaboration 
    }
}
