// Clase que guarda el resultado de la última partida (pasa información desde la escena
// de juego a la escena de gameover)
// static -> para poder utilizar la clase directamente mediante su nombre, sin crear un objeto
// ni tener que añadirla como componente.
using NUnit.Framework;

public static class GameResultData
{
    public static bool HasWon { get; private set; }
    public static string GameplaySceneName { get; private set; } = "Not_so_dark";

    public static void SetResult(bool hasWon, string gameplaySceneName)
    {
        HasWon = hasWon;
        GameplaySceneName = gameplaySceneName;
    }
}
