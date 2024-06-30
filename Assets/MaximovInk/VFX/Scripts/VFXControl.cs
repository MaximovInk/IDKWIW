using MaximovInk;
using UnityEngine;
using UnityEngine.VFX;

public class VFXControl : MonoBehaviour
{
    private VisualEffect _effect;

    public float LifeTime = -1;
    public bool AutoHideOnEmpty;

    private float _lifeTimeTimer = 0f;

    private void Awake()
    {
        _effect = GetComponent<VisualEffect>();
        if(_effect == null)
            Destroy(this);
    }

    private bool _isPlaying;

    public void Play()
    {
        _invokeHide = false;
        _effect.Play();
        _lifeTimeTimer = 0f;
        _isPlaying = true;
    }

    public void Stop()
    {
        _effect.Stop();
        _isPlaying = false;
        _lifeTimeTimer = 0f;
        _invokeHide = true;
    }

    private bool _invokeHide;

    private void Update()
    {
        if (LifeTime > 0f && _isPlaying)
        {
            _lifeTimeTimer += Time.deltaTime;
            if (_lifeTimeTimer > LifeTime)
            {
                Stop();
            }
        }

        if (_invokeHide && AutoHideOnEmpty && _effect.aliveParticleCount == 0)
        {
            gameObject.SetActive(false);
        }

    }
}
