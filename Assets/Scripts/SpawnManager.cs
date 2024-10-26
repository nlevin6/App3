using System.Collections;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public Transform[] spawnPoints;
    public GameObject enemyPrefab;
    public int enemiesPerRound = 3;
    public float spawnDelay = 1f;
    public float roundCooldown = 10f;

    private int currentRound = 1;
    private int totalEnemiesToSpawn;
    private int remainingEnemies;
    private bool roundInProgress = false;

    void Start()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Enemy Prefab is not assigned in SpawnManager!");
            return;
        }

        if (enemyPrefab.activeSelf)
        {
            enemyPrefab.SetActive(false);
        }

        StartNewRound();
    }

    void StartNewRound()
    {
        Debug.Log($"Starting Round: {currentRound}");
        roundInProgress = true;
        totalEnemiesToSpawn = enemiesPerRound * currentRound;
        remainingEnemies = totalEnemiesToSpawn;
        StartCoroutine(RoundCooldownAndSpawn());
    }

    IEnumerator RoundCooldownAndSpawn()
    {
        yield return new WaitForSeconds(roundCooldown);
        StartCoroutine(SpawnEnemies());
    }

    IEnumerator SpawnEnemies()
    {
        Debug.Log($"Spawning {totalEnemiesToSpawn} enemies for Round {currentRound}.");
        for (int i = 0; i < totalEnemiesToSpawn; i++)
        {
            if (!roundInProgress)
            {
                Debug.Log("Round ended unexpectedly. Stopping spawn.");
                yield break;
            }
            SpawnEnemy(i + 1);
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    void SpawnEnemy(int enemyNumber)
    {
        if (spawnPoints.Length == 0)
        {
            Debug.LogError("No spawn points assigned in the Inspector.");
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        enemy.SetActive(true);
        Debug.Log($"Spawned Enemy {enemyNumber} at {spawnPoint.position}.");

        MutantZombie mutantZombie = enemy.GetComponent<MutantZombie>();
        if (mutantZombie != null)
        {
            mutantZombie.OnDeath += EnemyDied;
        }
        else
        {
            Debug.LogWarning("Spawned enemy does not have a MutantZombie component.");
        }
    }

    void EnemyDied()
    {
        remainingEnemies--;
        Debug.Log($"Enemy died. Enemies left to kill: {remainingEnemies}");
        if (remainingEnemies <= 0 && roundInProgress)
        {
            EndRound();
        }
    }

    void EndRound()
    {
        if (!roundInProgress)
            return;

        Debug.Log($"Round {currentRound} complete.");
        roundInProgress = false;
        currentRound++;
        StartNewRound();
    }
}
