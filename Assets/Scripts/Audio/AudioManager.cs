using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
	// Muestra el nivel de alerta de los enemigos
	public enum nivelAlertaEnemigos
	{
		Normal = 1,
		Alerta = 2,
		Perseguir = 3
	}

	// Muestra en qué pantalla se está
	public enum estadoMenuJuego
	{
		EnJuego = 0,
		Inicio = 1,
		Pausa = 2,
		Ajustes = 3
	}

	// Se sepraan las pistas de audio en dos listas para tener uno con mayor prioridad que otro
	[SerializeField] private AudioSource audioActual;
	[SerializeField] private AudioClip audioNormal;
	[SerializeField] private AudioClip audioAlerta;
	[SerializeField] private AudioClip audioPerseguir;

	
	[SerializeField] private AudioClip audioInicio;
	[SerializeField] private AudioClip audioPausa; // ¿Cambiar este a la lista de juego?
	[SerializeField] private AudioClip audioAjustes; // Poner música de tienda
	

	private nivelAlertaEnemigos estadoEnemigosActual = nivelAlertaEnemigos.Normal;
	
	private estadoMenuJuego menuActual = estadoMenuJuego.EnJuego;
	

	// Diccionario que guarda el nivel de alerta de los enemigos por su ID
	private readonly Dictionary<int, nivelAlertaEnemigos> estadosDeEnemigos = new Dictionary<int, nivelAlertaEnemigos>();

	private void Start()
	{
		// Se activa el loop de serie
		if (audioActual != null)
		{
			audioActual.loop = true;
		}

		actualizarAudio();
	}

	// Los enemigos notificarán al script cuando se cambie de estado de comportamiento
	public void enemyNewState(int enemyId, nivelAlertaEnemigos newState)
	{
		// Cambia el valor de estado del enemigo
		estadosDeEnemigos[enemyId] = newState;
		recalcularEstadoGlobal();
	}

	// Lo pongo por sea caso
	public void eliminarEnemigo(int enemyId)
	{
		if (estadosDeEnemigos.Remove(enemyId)) // Se pone en el if para que en el caso de que no se pueda eliminar no se actualice la lista
		{
			recalcularEstadoGlobal();
		}
	}

	// Guarda en que estado de juego se encuentra la partida
	public void setEstadoMenu(estadoMenuJuego nuevoMenu)
	{
		if (menuActual == nuevoMenu) 
		{
			return;
		}

		menuActual = nuevoMenu;
		actualizarAudio();
		Debug.Log("El estado de juego ahora es: " + menuActual);
	}

	private void recalcularEstadoGlobal()
	{
		nivelAlertaEnemigos mayorPrioridad = nivelAlertaEnemigos.Normal;
		// Repasa los estados del diccionario para ver cual es el nuevo estado de mayor prioridad
		foreach (nivelAlertaEnemigos estadoEnemigo in estadosDeEnemigos.Values)
		{
			if (estadoEnemigo > mayorPrioridad)
			{
				mayorPrioridad = estadoEnemigo;
			}
		}
		estadoEnemigosActual = mayorPrioridad;

		// Solo cambia la música si se está en un menú con prioridad mayor
		if (menuActual == estadoMenuJuego.EnJuego)
		{
			actualizarAudio();
		}
	}

	private void actualizarAudio()
	{
		// Si se actualiza erroneamente no hace nada
		if (audioActual == null) 
		{ 
			return;
		}
		

		AudioClip targetAudio;

		// Se hacen dos ramas para separar el audio de fuera de la partida
		// con el de dentro de la partida, y hacer que tengan distintas prioridades

		// 1 - En caso de NO estar jugando
		if (menuActual != estadoMenuJuego.EnJuego)
		{

			targetAudio = menuActual switch
			{
				estadoMenuJuego.Inicio => audioInicio,
				estadoMenuJuego.Pausa => audioPausa,
				estadoMenuJuego.Ajustes => audioAjustes,
				_ => audioInicio
			};
		}

		else
		// 2 - En caso de SÍ estar jugando
		{
			targetAudio = estadoEnemigosActual switch
			{
				nivelAlertaEnemigos.Normal => audioNormal,
				nivelAlertaEnemigos.Alerta => audioAlerta,
				nivelAlertaEnemigos.Perseguir => audioPerseguir,
				_ => audioNormal
			};
		}

		if (targetAudio != null && audioActual.clip != targetAudio)
		{
			audioActual.Stop();
			audioActual.clip = targetAudio;
			audioActual.Play();
		}
		else if (targetAudio == null)
		{
			Debug.LogWarning("No se ha encontrado el audio para " + targetAudio);
		}
	}

}