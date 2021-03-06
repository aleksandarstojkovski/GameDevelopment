﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpecialCoinRotation : MonoBehaviour
{

    public bool goUp;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (goUp == true)
        {
            transform.Rotate(0, 0, 0);
            transform.Translate(0, 0, 0.4f);
        }
        else
        {
            transform.Rotate(0, 0, 1.5f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            goUp = true;
            Messenger.Broadcast(GameEvent.PLAY_COIN_SOUND, MessengerMode.DONT_REQUIRE_LISTENER);
        }
    }
}
