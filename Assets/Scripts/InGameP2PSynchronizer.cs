using System.Collections;
using MessagePack;
using PhantomCatWorks.RealtimeP2PKit;
using IsoTools.Examples.Kenney;
using UnityEngine;
using IsoTools;

/// <summary>
/// Online room matches only: synchronizes each player's character using
/// IsoObject.position, which is the coordinate system used by IsoWorld.
/// </summary>
public sealed class InGameP2PSynchronizer : MonoBehaviour
{
    private const byte InGameStatePacketId = 4;
    private const float PositionSendIntervalSeconds = 1f / 20f;
    private const float CaptureResendIntervalSeconds = 0.2f;
    private const int CaptureResendCount = 10;
    private const float CaptureEndingDelaySeconds = 3f;

    [MessagePackObject(AllowPrivate = true)]
    internal struct InGameStatePacket
    {
        [Key(0)] public int Character;
        [Key(1)] public float IsoX;
        [Key(2)] public float IsoY;
        [Key(3)] public float IsoZ;
        [Key(4)] public bool PhantomCatCaught;
    }

    private Transform policeDog;
    private CatSpawner catSpawner;
    private PlayableCharacter localCharacter;
    private CatDetector policeDogDetector;
    private bool online;
    private bool matchFinished;
    private float nextPositionSendTime;
    private bool hasRemotePosition;
    private Vector3 remoteIsoPosition;

    public void Initialize(Transform policeDogTransform, CatSpawner spawner, PlayableCharacter selectedCharacter)
    {
        policeDog = policeDogTransform;
        catSpawner = spawner;
        localCharacter = selectedCharacter;
    }

    private void Start()
    {
        var p2pManager = P2PManager.Instance;
        online = p2pManager.IsOnlineMatch
            && p2pManager.Session.State == P2PSessionState.Connected;
        if (!online || policeDog == null || catSpawner == null)
        {
            enabled = false;
            return;
        }

        p2pManager.RegisterPacketHandler<InGameStatePacket>(InGameStatePacketId, OnRemoteStateReceived);

        // Only the locally controlled dog has an enabled detector, but subscribe
        // defensively so a successful capture is propagated immediately.
        policeDogDetector = policeDog.GetComponent<CatDetector>();
        if (policeDogDetector != null)
        {
            policeDogDetector.PhantomCatCaught += OnLocalPhantomCatCaught;
        }

        Debug.Log("[InGameP2P] IsoWorld position and capture-result synchronization enabled.");
    }

    private void Update()
    {
        if (!online)
        {
            return;
        }

        ApplyRemotePosition();

        if (!matchFinished && Time.unscaledTime >= nextPositionSendTime)
        {
            SendLocalPosition();
            nextPositionSendTime = Time.unscaledTime + PositionSendIntervalSeconds;
        }
    }

    private void SendLocalPosition()
    {
        var localTransform = GetCharacterTransform(localCharacter);
        if (localTransform == null || !localTransform.TryGetComponent<IsoObject>(out var isoObject))
        {
            return;
        }

        var position = isoObject.position;
        P2PManager.Instance.Send(InGameStatePacketId, new InGameStatePacket
        {
            Character = (int)localCharacter,
            IsoX = position.x,
            IsoY = position.y,
            IsoZ = position.z,
            PhantomCatCaught = false,
        });
    }

    private void OnRemoteStateReceived(InGameStatePacket packet)
    {
        if (packet.Character != (int)RemoteCharacter)
        {
            return;
        }

        // Capture-result packets intentionally omit coordinates, so do not
        // overwrite the last valid IsoWorld position with their default zeros.
        if (!packet.PhantomCatCaught)
        {
            remoteIsoPosition = new Vector3(packet.IsoX, packet.IsoY, packet.IsoZ);
            hasRemotePosition = true;
        }

        if (packet.PhantomCatCaught)
        {
            OnRemotePhantomCatCaught();
        }
    }

    private void ApplyRemotePosition()
    {
        if (!hasRemotePosition)
        {
            return;
        }

        var remoteTransform = GetCharacterTransform(RemoteCharacter);
        if (remoteTransform != null && remoteTransform.TryGetComponent<IsoObject>(out var isoObject))
        {
            isoObject.position = remoteIsoPosition;
        }
    }

    private void OnLocalPhantomCatCaught()
    {
        if (!online || localCharacter != PlayableCharacter.PoliceDog || matchFinished)
        {
            return;
        }

        matchFinished = true;
        StartCoroutine(ResendCaptureResult());
    }

    private IEnumerator ResendCaptureResult()
    {
        for (var i = 0; i < CaptureResendCount; i++)
        {
            P2PManager.Instance.Send(InGameStatePacketId, new InGameStatePacket
            {
                Character = (int)localCharacter,
                PhantomCatCaught = true,
            });
            yield return new WaitForSecondsRealtime(CaptureResendIntervalSeconds);
        }
    }

    private void OnRemotePhantomCatCaught()
    {
        if (matchFinished)
        {
            return;
        }

        matchFinished = true;

        // The peer that received this result controls PhantomCat. Stop only its
        // local input, then use the existing failure ending after the same delay
        // as the dog side's successful-capture sequence.
        if (localCharacter == PlayableCharacter.PhantomCat)
        {
            var localCat = catSpawner.PhantomCat;
            if (localCat != null && localCat.TryGetComponent<CatController>(out var catController))
            {
                catController.enabled = false;
            }

            StartCoroutine(MoveCapturedPlayerToEnding());
        }
    }

    private IEnumerator MoveCapturedPlayerToEnding()
    {
        yield return new WaitForSeconds(CaptureEndingDelaySeconds);
        if (GameManager.Instance != null)
        {
            GameManager.Instance.MoveToFailScene();
        }
    }

    private Transform GetCharacterTransform(PlayableCharacter character)
    {
        if (character == PlayableCharacter.PoliceDog)
        {
            return policeDog;
        }

        return catSpawner != null && catSpawner.PhantomCat != null
            ? catSpawner.PhantomCat.transform
            : null;
    }

    private PlayableCharacter RemoteCharacter =>
        localCharacter == PlayableCharacter.PhantomCat
            ? PlayableCharacter.PoliceDog
            : PlayableCharacter.PhantomCat;

    private void OnDestroy()
    {
        if (policeDogDetector != null)
        {
            policeDogDetector.PhantomCatCaught -= OnLocalPhantomCatCaught;
        }

        if (online)
        {
            P2PManager.Instance.UnregisterPacketHandler(InGameStatePacketId);
        }
    }
}
