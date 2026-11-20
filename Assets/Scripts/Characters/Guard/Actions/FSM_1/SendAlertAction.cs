// Acción del estado MandarMonstruo. Se encarga de enviar el aviso cuando el guardia
// ve al jugador.
public class SendAlertAction
{
    private readonly MuseumGuardVision _vision;
    private readonly MuseumGuardPatrolRunner.GuardSightingEvent _onPlayerSpotted;

    public SendAlertAction(MuseumGuardVision vision, MuseumGuardPatrolRunner.GuardSightingEvent onPlayerSpotted)
    {
        _vision = vision;
        _onPlayerSpotted = onPlayerSpotted;
    }

    // Comunica el aviso al monstruo
    public void Begin()
    {
        // se marca el aviso como enviado
        _vision.MarkSightingReported();
        // y se envían los datos del id de la sala donde está el guardia y la posición del jugador
        _onPlayerSpotted.Invoke(_vision.SightingRoomID, _vision.SightingPosition);
    }
}
