using GameNetcodeStuff;
using UnityEngine;

public class KillLocalPlayer : MonoBehaviour
{
	public bool dontSpawnBody;

	public CauseOfDeath causeOfDeath = CauseOfDeath.Gravity;

	public bool justDamage;

	public StartOfRound playersManager;

	public int deathAnimation;

	[Space(5f)]
	public RoundManager roundManager;

	public Transform spawnEnemyPosition;

	[Space(5f)]
	public int enemySpawnNumber;

	public int playAudioOnDeath = -1;

	public GameObject spawnPrefab;

	public void KillPlayer(PlayerControllerB playerWhoTriggered)
	{
		if (justDamage)
		{
			playerWhoTriggered.DamagePlayer(25);
			Debug.Log("DD TRIGGER");
			return;
		}
		if (playAudioOnDeath != -1)
		{
			SoundManager.Instance.PlayAudio1AtPositionForAllClients(playerWhoTriggered.transform.position, playAudioOnDeath);
		}
		if (spawnPrefab != null)
		{
			Object.Instantiate(spawnPrefab, playerWhoTriggered.lowerSpine.transform.position, Quaternion.identity, RoundManager.Instance.mapPropsContainer.transform);
		}
		playerWhoTriggered.KillPlayer(Vector3.zero, !dontSpawnBody, causeOfDeath, deathAnimation);
	}

	public void SpawnEnemy()
	{
		if (GameNetworkManager.Instance.localPlayerController.playerClientId == 0L)
		{
			roundManager.SpawnEnemyOnServer(spawnEnemyPosition.position, 0f, enemySpawnNumber);
		}
	}
}
