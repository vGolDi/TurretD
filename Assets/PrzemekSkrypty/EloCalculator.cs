using UnityEngine;

namespace ElementumDefense.Ranked
{
    /// <summary>
    /// Statyczny kalkulator ELO — formuła Elo,
    /// buckety matchmakingowe, definicje rang.
    /// </summary>
    public static class EloCalculator
    {
        // ==========================================
        // STAŁE
        // ==========================================

        /// Rozmiar jednego bucketa matchmakingowego
        public const int BUCKET_SIZE = 200;

        /// Maks. odległość bucketów przy szukaniu
        public const int MAX_BUCKET_DISTANCE = 1;

        /// Domyślne ELO nowego gracza
        public const int DEFAULT_ELO = 1000;

        /// Minimalne ELO (nie zejdzie poniżej)
        public const int MIN_ELO = 0;

        /// Gwarantowana min. zmiana ELO za grę
        public const int MIN_CHANGE = 5;

        /// Maks. zmiana ELO za jedną grę
        public const int MAX_CHANGE = 50;

        // ==========================================
        // KALKULACJA ELO
        // ==========================================

        /// <summary>
        /// Oblicza zmianę ELO na podstawie formuły
        /// Elo z K-factorem zależnym od rangi.
        ///
        /// Jeśli pokonasz kogoś z wyższym ELO
        /// → dostajesz więcej.
        /// Jeśli przegrasz z kimś niżej
        /// → tracisz więcej.
        /// </summary>
        public static int CalculateEloChange(
            int playerElo,
            int opponentElo,
            bool won)
        {
            int K = GetKFactor(playerElo);

            // Oczekiwany wynik (0.0 – 1.0)
            float expected = 1f / (1f + Mathf.Pow(
                10f,
                (opponentElo - playerElo) / 400f));

            float actual = won ? 1f : 0f;

            int change = Mathf.RoundToInt(
                K * (actual - expected));

            // Gwarantuj sensowny zakres
            if (won)
                change = Mathf.Clamp(
                    change, MIN_CHANGE, MAX_CHANGE);
            else
                change = Mathf.Clamp(
                    change, -MAX_CHANGE, -MIN_CHANGE);

            return change;
        }

        /// <summary>
        /// K-factor: wyższy w niskich rangach
        /// (szybsze zmiany), niższy w wysokich
        /// (stabilniejsze rankingi).
        /// </summary>
        public static int GetKFactor(int elo)
        {
            if (elo < 1200) return 40;   // Bronze
            if (elo < 1500) return 32;   // Silver
            if (elo < 1800) return 28;   // Gold
            if (elo < 2200) return 24;   // Platinum
            return 20;                    // Diamond
        }

        // ==========================================
        // MATCHMAKING BUCKETY
        // ==========================================

        /// <summary>
        /// Bucket matchmakingowy gracza.
        /// Np. ELO 1200 → bucket 6,
        ///     ELO 1500 → bucket 7.
        /// </summary>
        public static int GetBucket(int elo)
        {
            return Mathf.Max(0, elo / BUCKET_SIZE);
        }

        /// <summary>
        /// Czy dwóch graczy może być sparowanych?
        /// Porównuje odległość bucketów.
        /// </summary>
        public static bool CanMatch(
            int elo1,
            int elo2,
            int maxDistance = -1)
        {
            if (maxDistance < 0)
                maxDistance = MAX_BUCKET_DISTANCE;

            int b1 = GetBucket(elo1);
            int b2 = GetBucket(elo2);
            return Mathf.Abs(b1 - b2) <= maxDistance;
        }

        /// <summary>
        /// Zakres ELO widoczny w szukaniu meczu.
        /// Zwraca (minElo, maxElo) dla danego
        /// bucketa ± maxDistance.
        /// </summary>
        public static (int min, int max)
            GetSearchRange(
                int elo,
                int maxDistance = -1)
        {
            if (maxDistance < 0)
                maxDistance = MAX_BUCKET_DISTANCE;

            int bucket = GetBucket(elo);
            int minBucket =
                Mathf.Max(0, bucket - maxDistance);
            int maxBucket = bucket + maxDistance;

            int minElo = minBucket * BUCKET_SIZE;
            int maxElo =
                (maxBucket + 1) * BUCKET_SIZE - 1;

            return (minElo, maxElo);
        }

        // ==========================================
        // NAZWY RANG (Z SUB-TIEREM)
        // ==========================================

        /// <summary>
        /// Pełna nazwa rangi z sub-tierem:
        /// "BRONZE I", "SILVER III", "GOLD II" itd.
        /// </summary>
        public static string GetRankName(int elo)
        {
            if (elo < 400) return "BRONZE I";
            if (elo < 800) return "BRONZE II";
            if (elo < 1200) return "BRONZE III";
            if (elo < 1300) return "SILVER I";
            if (elo < 1400) return "SILVER II";
            if (elo < 1500) return "SILVER III";
            if (elo < 1600) return "GOLD I";
            if (elo < 1700) return "GOLD II";
            if (elo < 1800) return "GOLD III";
            if (elo < 1933) return "PLATINUM I";
            if (elo < 2067) return "PLATINUM II";
            if (elo < 2200) return "PLATINUM III";
            if (elo < 2500) return "DIAMOND I";
            if (elo < 2800) return "DIAMOND II";
            return "DIAMOND III";
        }

        /// <summary>
        /// Główna nazwa rangi (bez sub-tieru):
        /// "BRONZE", "SILVER", "GOLD" itd.
        /// </summary>
        public static string GetMainRankName(
            int elo)
        {
            if (elo < 1200) return "BRONZE";
            if (elo < 1500) return "SILVER";
            if (elo < 1800) return "GOLD";
            if (elo < 2200) return "PLATINUM";
            return "DIAMOND";
        }

        // ==========================================
        // KOLORY RANG
        // ==========================================

        /// <summary>
        /// Kolor rangi (dla UI).
        /// </summary>
        public static Color GetRankColor(int elo)
        {
            if (elo < 1200)
                return new Color(
                    0.8f, 0.5f, 0.2f);   // Bronze
            if (elo < 1500)
                return new Color(
                    0.75f, 0.75f, 0.75f); // Silver
            if (elo < 1800)
                return new Color(
                    1f, 0.84f, 0f);       // Gold
            if (elo < 2200)
                return new Color(
                    0f, 1f, 1f);          // Platinum
            return new Color(
                0.7f, 0.2f, 1f);          // Diamond
        }

        // ==========================================
        // ZAKRESY RANG
        // ==========================================

        /// <summary>
        /// Zakres ELO głównej rangi
        /// (do paska postępu w UI).
        /// </summary>
        public static (int min, int max)
            GetRankRange(int elo)
        {
            if (elo < 1200) return (0, 1200);
            if (elo < 1500) return (1200, 1500);
            if (elo < 1800) return (1500, 1800);
            if (elo < 2200) return (1800, 2200);
            return (2200, 3000);
        }

        /// <summary>
        /// Zakres ELO sub-tieru.
        /// </summary>
        public static (int min, int max)
            GetSubTierRange(int elo)
        {
            if (elo < 400) return (0, 400);
            if (elo < 800) return (400, 800);
            if (elo < 1200) return (800, 1200);
            if (elo < 1300) return (1200, 1300);
            if (elo < 1400) return (1300, 1400);
            if (elo < 1500) return (1400, 1500);
            if (elo < 1600) return (1500, 1600);
            if (elo < 1700) return (1600, 1700);
            if (elo < 1800) return (1700, 1800);
            if (elo < 1933) return (1800, 1933);
            if (elo < 2067) return (1933, 2067);
            if (elo < 2200) return (2067, 2200);
            if (elo < 2500) return (2200, 2500);
            if (elo < 2800) return (2500, 2800);
            return (2800, 3500);
        }

        // ==========================================
        // NUMERAL (DO UI)
        // ==========================================

        /// <summary>
        /// Numeral rangi: "I", "II", "III".
        /// </summary>
        public static string GetRankNumeral(
            int elo)
        {
            string name = GetRankName(elo);
            if (name.EndsWith("III")) return "III";
            if (name.EndsWith("II")) return "II";
            return "I";
        }
    }
}