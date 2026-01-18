using System;
using Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Gameplay
{
    public class PlinkoBall : MonoBehaviour, IPoolable
    {
        private float _baseBounceValue = 0.6f;
        private float _bounceVariation = 0.1f;
        private float _angularDrag = 0.5f;
        private float _maxVelocity = 15f;

        private Rigidbody2D _rigidbody;
        private CircleCollider2D _collider;
        private TrailRenderer _trailRenderer;
        
        private bool _isActive;
        private float _spawnTime;
        private const float MAX_LIFETIME = 10f;

        public Action<PlinkoBall, int> OnBucketEntered;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<CircleCollider2D>();
            _trailRenderer = GetComponent<TrailRenderer>();

            SetupPhysics();
        }

        private void FixedUpdate()
        {
            if (!_isActive)
                return;

            // Cap velocity for stability
            if (_rigidbody.linearVelocity.magnitude > _maxVelocity)
            {
                _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * _maxVelocity;
            }

            // Auto-recycle if stuck too long
            if (Time.time - _spawnTime > MAX_LIFETIME)
            {
                OnDespawn();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!_isActive)
                return;

            // Add bounce variation for natural feel
            if (collision.gameObject.CompareTag("Peg"))
            {
                float variation = Random.Range(-_bounceVariation, _bounceVariation);
                Vector2 reflectDir = Vector2.Reflect(_rigidbody.linearVelocity.normalized, collision.contacts[0].normal);
                _rigidbody.linearVelocity = reflectDir * _rigidbody.linearVelocity.magnitude * (_baseBounceValue + variation);

                // Add slight spin
                _rigidbody.angularVelocity += Random.Range(-50f, 50f);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_isActive)
                return;

            if (!other.CompareTag("Bucket"))
                return;
            
            PlinkoBucket bucket = other.GetComponent<PlinkoBucket>();
            if (bucket != null)
            {
                OnBucketEntered?.Invoke(this, bucket.BucketIndex);
            }
        }

        public void OnSpawn()
        {
            gameObject.SetActive(true);
            _isActive = true;
            _spawnTime = Time.time;

            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;

            // Add slight random initial velocity for variation
            float randomX = Random.Range(-0.5f, 0.5f);
            _rigidbody.linearVelocity = new Vector2(randomX, 0f);

            if (_trailRenderer)
            {
                _trailRenderer.Clear();
                _trailRenderer.enabled = true;
            }
        }

        public void OnDespawn()
        {
            _isActive = false;

            if (_trailRenderer)
                _trailRenderer.enabled = false;

            OnBucketEntered = null;
            gameObject.SetActive(false);
        }

        public void EnableTrailRenderer(bool isActive)
        {
            if (!_trailRenderer)
                return;
            
            _trailRenderer.enabled = isActive;
        }

        private void SetupPhysics()
        {
            if (!_rigidbody)
                return;

            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rigidbody.angularDamping = _angularDrag;
            _rigidbody.gravityScale = 1f;
        }
    }
}