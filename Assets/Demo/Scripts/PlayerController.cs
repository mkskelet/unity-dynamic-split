using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public Transform[] Players;
    public float Speed = 2.5f;

    private Rigidbody2D[] playerRigidbody;

    private int activePlayerIndex = 0;
    private int lastActivePlayerIndex = 0;

    private void Awake()
    {
        playerRigidbody = new Rigidbody2D[Players.Length];

        for (int i = 0; i < Players.Length; i++)
        {
            playerRigidbody[i] = Players[i].GetComponent<Rigidbody2D>();
        }
    }

    private void Update()
    {
        // switch players
        if (Input.GetKeyUp(KeyCode.Alpha1)) activePlayerIndex = 0;
        if (Input.GetKeyUp(KeyCode.Alpha2)) activePlayerIndex = 1;

        // stop last player
        if (lastActivePlayerIndex != activePlayerIndex)
        {
            if (lastActivePlayerIndex < Players.Length)
            {
                playerRigidbody[lastActivePlayerIndex].velocity = Vector2.zero;
            }

            lastActivePlayerIndex = activePlayerIndex;
        }

        // no player to control
        if (activePlayerIndex >= Players.Length) return;

        // control player
        Vector3 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical")).normalized * Speed;
        playerRigidbody[activePlayerIndex].velocity = movement;
    }
}
