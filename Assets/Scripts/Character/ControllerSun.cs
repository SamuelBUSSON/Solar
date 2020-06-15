﻿using System;
using System.Collections;
using System.Collections.Generic;
using BeardedManStudios.Forge.Networking;
using BeardedManStudios.Forge.Networking.Generated;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ControllerSun : MonoBehaviour
{
    [Header("Sun control")]
    private LightController _sun;
    [SerializeField, Range(0,100)] public float _maxRotateSpeed = 100f;
    [SerializeField, Range(0,100)] private float _speedVelocity = 5;
    private float _time;
    // Variable définissant la position que dois atteindre le soleil. 
    // Elle est modifié par le controlleur d'input
    //[HideInInspector]
    public float _gotoAngle = 0;
    
    // Aiguilles de l'astrolabe
    [SerializeField] private GameObject _gotoAngleInfo;
    public float _angle = 0;
    [SerializeField] private GameObject _angleInfo;
    private float _angleVelocity = 0;
    
    [Header("Player Reaction with Sun")]
    [SerializeField] private float _life = 1;
    public float Life => _life;
    [SerializeField] private List<Point> _points;
    [SerializeField] private Gradient fx;
    [SerializeField] private AnimationCurve _pulsate;
    private float _pulsateSpeed = 2;
    [SerializeField] public Image fxUI;
    [SerializeField]  private float _speedHeal=1f;
    public PostProcessEffect ppeffect;

    private Controller _controller;
    private int _testPoint;

    public bool vibrate;
    
    void Awake()
    {
        ppeffect = FindObjectOfType<PostProcessEffect>();
        
        _sun = FindObjectOfType<LightController>();
        _controller = GetComponent<Controller>();
        _points = new List<Point>();
        GetPoints(this.gameObject);
        
        _gotoAngle = _sun.transform.eulerAngles.y;
        _angle = _gotoAngle;
    }


    void Update()
    {
        // Ne fait rien si le joueur est mort 
        if (_controller.IsDead()) return;
        
        // Défini la vitesse du soleil et met à jour la position du soleil
        float rotateSpeed = DefineSpeed();
        _angle += rotateSpeed * Time.deltaTime;
        AkSoundEngine.SetRTPCValue("RTPC_Sun_Velocity", Mathf.Abs(rotateSpeed / _maxRotateSpeed));
        Vector3 sunEuler = _sun.transform.eulerAngles;
        sunEuler.y = _angle;
        _sun.transform.eulerAngles = sunEuler;

        // Met à jour les aiguilles de l'astrolabe
        _angleInfo.transform.localEulerAngles = new Vector3(0, -_angle, 0);
        _gotoAngleInfo.transform.localEulerAngles = new Vector3(0, -_gotoAngle, 0);

        // Calcul la vie actuelle du personnage
        SetLife();
        
    }
    
    /// <summary>
    /// Défini la vélocité du soleil en fonction de la position de l'astrolable.
    /// </summary>
    float DefineSpeed()
    {
        /*
        //Debug.Log(Mathf.Abs(_gotoAngle - _angle)+ " " + Mathf.Abs(_angleVelocity) + " " + _maxRotateSpeed);
        if (Mathf.Abs(_gotoAngle - _angle) > Mathf.Abs(_angleVelocity)/2 && Mathf.Abs(_angleVelocity) < _maxRotateSpeed)
            _angleVelocity = Mathf.Min(Mathf.Abs(_gotoAngle - _angle)/Time.deltaTime , Mathf.Abs(_angleVelocity) + _speedVelocity) * Mathf.Sign(_gotoAngle-_angle);
        else if (Mathf.Abs(_gotoAngle - _angle) <= _maxRotateSpeed * Time.deltaTime) _angleVelocity = 0;
        return _angleVelocity;
        */
        
        
        // _gotoAngle - _angle - _angleVelocity - _maxRotateSpeed
        float _maxActualSpeed = Mathf.Min(_maxRotateSpeed * _controller.UiInterface.player2Settings.sunSensitivity , Mathf.Abs(_gotoAngle - _angle) / Time.deltaTime);
        _angleVelocity = Mathf.Min(_maxActualSpeed, Mathf.Abs(_angleVelocity) + _speedVelocity* _controller.UiInterface.player2Settings.sunSensitivity * Time.deltaTime) * Mathf.Sign(_gotoAngle - _angle);




        return _angleVelocity;
    }


    /// <summary>
    ///  Calcul la vie actuel du personnage à l'aide des points de contact.
    /// </summary>
    private void SetLife()
    {
        
        if (!_controller.activeDead) _life = 1;
        else{
            bool touch = false;
            for (int i = 0; i < _points.Count; ++i)
            {
                _life -= _points[i].TestLight(_sun,_testPoint == i) * Time.deltaTime;
                touch |= _points[i].Touch;
            }

            if (!touch) _life  = Mathf.Min(1, _life +_speedHeal * Time.deltaTime);

            else if (_life <= 0 && _controller.inputs.CouldDie())
            {
                _controller.Dying();
            }
            if(_life < 0) _life = 0;
        }
        
        AkSoundEngine.SetRTPCValue("RTPC_Distance_Sun", Mathf.Abs(_life * 100));

        // Feedback visuel
        _time = (_time + Time.deltaTime * _pulsateSpeed * (1 - _life)) % 1;
        fxUI.color = fx.Evaluate(1 - _life) * new Color(1, 1, 1, 0.8f + _pulsate.Evaluate(_time) * 0.2f);
        ppeffect.Interpolate(1-_life);
        
        
        // Incrémente le point à vérifier à la prochaine frame
        ++_testPoint;
        if (_testPoint == _points.Count) _testPoint = 0;


        if (_controller.UiInterface.player1Settings.vibration && vibrate && _life < 0.8f && _life > 0)
            Gamepad.current.SetMotorSpeeds((1 - _life - 0.2f) * 1.25f *0.5f, (1 - _life - 0.2f) * 1.25f*0.5f);
        else Gamepad.current.SetMotorSpeeds(0, 0);
    }
    
    
    /// <summary>
    ///  Calcul la vie actuel du personnage à l'aide des points de contact.
    /// </summary>
    private void LastSetLife()
    {
        // Calcul la vie en fonction des points en contact avec le soleil 
        _life = _points.Count;

        for (int i = 0; i < _points.Count; ++i)
        {
            _life -= _points[i].TestLight(_sun,_testPoint == i);
        }
        
        _life /= _points.Count;
        
        
        // Active la mort du personnage si sa vie atteint zéro
        if (_life <= 0)
        {
            _controller.Dying();
        }
    }
    
    /// <summary>
    /// Cherche parmis les fils du GameObject les points de contact 
    /// </summary>
    private void GetPoints(GameObject obj)
    {
        
        if(obj.TryGetComponent<Point>(out var p)) _points.Add(p);
        for (int i = 0; i < obj.transform.childCount; ++i)
        {
            GetPoints(obj.transform.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// Reinitialise la vie et les points de contact
    /// </summary>
    public void ResetPoints()
    {
        _life = 1;
        foreach (Point p in _points)
        {
            p.ResetPoint();
        }
    }

    /// <summary>
    /// Réinitialise la rotation du soleil
    /// </summary>
    public void ResetRotate(float angle)
    {
        _gotoAngle = angle;
        _angle = angle;
    }
}
