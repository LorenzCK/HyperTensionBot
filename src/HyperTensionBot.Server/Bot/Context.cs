using HyperTensionBot.Server.ModelML;

namespace HyperTensionBot.Server.Bot {
    public static class Context {

        public static void controlFlow(Intent context) {
            switch (context) {
                case Intent.richiestaStatsFreq:
                case Intent.richiestaStatsPress:
                case Intent.richiestaStatsGener:
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
