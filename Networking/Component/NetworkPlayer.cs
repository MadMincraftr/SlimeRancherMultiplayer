﻿using Mirror;
using SRMP.Networking.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SRMP.Networking.Component
{
    public class NetworkPlayer : MonoBehaviour
    {
        public int id;
        float transformTimer = 0.1f;
        /// <summary>
        /// Player Preview Camera
        /// </summary>
        public Camera cam;

        /// <summary>
        /// Last region check position. Used on host for calculating if the regions should be updated.
        /// </summary>
        public Vector3 lastRegionCheckPos;
        internal void InitCamera()
        {
            cam = gameObject.GetComponentInChildren<Camera>();
        }

        void OnDestroy()
        {
            if (this.IsHandling()) return;

            var msg = new PlayerLeaveMessage()
            {
                id = id,
            };
            SRNetworkManager.NetworkSend(msg);
        }

        /// <summary>
        /// Use this to preview the player camera.
        /// </summary>
        public void StartCamera()
        {
            MultiplayerManager.Instance.currentPreviewRenderer = this;
            cam.enabled = true;
            cam.targetTexture = MultiplayerManager.Instance.playerCameraPreviewImage;
            MultiplayerManager.Instance.AddPreviewToUI();
        }

        /// <summary>
        /// Use this to freeze the preview for this player.
        /// </summary>
        public void StopCamera()
        {
            cam.enabled = false;
            cam.targetTexture = null;
        }

        public void Update()
        {
            transformTimer -= Time.unscaledDeltaTime;
            if (transformTimer < 0)
            {
                transformTimer = 0.1f;


                var packet = new PlayerUpdateMessage()
                {
                    id = id,
                    pos = transform.position,
                    rot = transform.rotation
                };
                SRNetworkManager.NetworkSend(packet);
            }
            
        }
    }
}
