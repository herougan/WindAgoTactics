
using UnityEngine;

public static class GeneralUtil {

    public static bool Within(int x, int lim1, int lim2) {
        return ((lim1 < x) && (x < lim2));
    }
    public static bool Covered(int x, int lim1, int lim2) {
        return ((lim1 <= x) && (x <= lim2));
    }
}

