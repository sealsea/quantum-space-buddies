﻿using QSB.Animation;
using UnityEngine;
using UnityEngine.Networking;

namespace QSB.TransformSync
{
    public class PlayerTransformSync : TransformSync
    {
        public static PlayerTransformSync LocalInstance { get; private set; }

        static PlayerTransformSync()
        {
            AnimControllerPatch.Init();
        }

        public override void OnStartLocalPlayer()
        {
            LocalInstance = this;
        }

        private Transform GetPlayerModel()
        {
            return Locator.GetPlayerTransform().Find("Traveller_HEA_Player_v2");
        }

        protected override Transform InitLocalTransform()
        {
            var body = GetPlayerModel();

            GetComponent<AnimationSync>().InitLocal(body);

            return body;
        }

        protected override Transform InitRemoteTransform()
        {
            var body = Instantiate(GetPlayerModel());

            GetComponent<AnimationSync>().InitRemote(body);

            var marker = body.gameObject.AddComponent<PlayerHUDMarker>();
            marker.Init(Player);

            return body;
        }

        public override bool IsReady => Locator.GetPlayerTransform() != null 
            && Player != null 
            && PlayerRegistry.PlayerExists(Player.PlayerId) 
            && Player.IsReady
            && netId.Value != uint.MaxValue
            && netId.Value != 0U;
    }
}
