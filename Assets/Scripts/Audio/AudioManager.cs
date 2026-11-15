using UnityEngine;

public class AudioManager : MonoBehaviour
{
	public enum nivelAlertaEnemigos
	{
		Normal = 1,
		Alerta = 2,
		Chasing = 3
	}

	[SerializeField] private AudioSource audioActual;
	[SerializeField] private AudioClip audioNormal;
	[SerializeField] private AudioClip audioAlerta;
	[SerializeField] private AudioClip audioPerseguir;

	private nivelAlertaEnemigos estadoActual = nivelAlertaEnemigos.Normal;

	private void Start()
	{
		// Se establece el "audioActual" inicialmente
		if (audioActual != null && audioNormal != null) // Se comprueban que los dos estén asignados
		{
			audioActual.clip = audioNormal;
			audioActual.loop = true;
			audioActual.Play();
		} else {
			Debug.LogWarning("No se ha encontrado el archivo 'audioNormal'");
		}
	}

	// Los enemigos notificarán al script cuando se cambie de estado de comportamiento
	public void enemyNewState(nivelAlertaEnemigos newState)
	{
		// Pese a que los enemigos estén en zonas separadas,
		// se da prioridad al orden de los audios para alertar al jugador de su alrededor
		if (newState > estadoActual)
		{
			setNewState(newState);
		}
	}

	public void setNewState(nivelAlertaEnemigos newState) // Se deja público para las interfaces [BORRAR]
	{
		if (estadoActual == newState) 
		{
			return;
		}


		estadoActual = newState;
		cambiarAudio(newState);
		Debug.Log("El estado de alerta ahora es de " + estadoActual);
	}

	private void cambiarAudio(nivelAlertaEnemigos state)
	{
		if (audioActual == null) return;

		AudioClip targetAudio = state switch // Traduce el nivel de alerta al audio requerido
		{
			nivelAlertaEnemigos.Normal => audioNormal,
			nivelAlertaEnemigos.Alerta => audioAlerta,
			nivelAlertaEnemigos.Chasing => audioPerseguir,
			_ => audioNormal
		};

		if (targetAudio != null && audioActual.clip != targetAudio)
		{
			audioActual.Stop();
			audioActual.clip = targetAudio;
			audioActual.Play();
		}
	}
}