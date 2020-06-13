using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class KinectRzutScript : MonoBehaviour
{
    //	public Vector3 TopLeft;
    //	public Vector3 TopRight;
    //	public Vector3 BottomRight;
    //	public Vector3 BottomLeft;
    private ThrowListener throwListener;
    private bool ballThrew = false;
    //pozycja ramienia
    private Vector3 userShoulderRightPos, userShoulderLeftPos, oldUserShoulderRightPos, oldUserShoulderLeftPos;
    //pozycja redlo w czasie t i t-1;
    private Vector3 userHandPos, oldUserHandPos, lastUserHandPos, startUserHandPos;
    private Vector3 oldBallPos;
    //dlugosc ruchu w czasie t - t-1 i t-1 - t-2
    public float distance, oldDistance, dist2old;
    public float speedx, speedy, speedz, oldSpeed;
    private float acceleration = 2, fullAcceleration;
    private float angle, speed, uhpx, uhpy, ts, zT1, uhp1y;
    private float timestamp, oldTimestamp, startTimestamp;
    private bool isThrow = false;
    private int state = 0;
    private Vector2 startBallPos;

    public TextScript textbox;

    private string throwPara;

    public GUITexture backgroundImage;
    public KinectWrapper.NuiSkeletonPositionIndex TrackedJoint = KinectWrapper.NuiSkeletonPositionIndex.HandRight;
    public KinectWrapper.NuiSkeletonPositionIndex TrackedJointShoulderRight = KinectWrapper.NuiSkeletonPositionIndex.ShoulderRight;
    public KinectWrapper.NuiSkeletonPositionIndex TrackedJointShoulderLeft = KinectWrapper.NuiSkeletonPositionIndex.ShoulderLeft;
    public GameObject OverlayObject;
    public Rigidbody Rigidbody;
    public float smoothFactor = 1f;
    public float count = 1;

    public GUIText debugText;

    private float distanceToCamera = 10f;


    void Start()
    {
        this.throwListener = Camera.main.GetComponent<ThrowListener>();

        //this.textbox.info = "Wykonaj gest rzutu prawa reka.";

        if (OverlayObject)
        {
            distanceToCamera = (OverlayObject.transform.position - Camera.main.transform.position).magnitude;
        }
    }



    void Update()
    {

        KinectManager manager = KinectManager.Instance;

        manager.smoothing = KinectManager.Smoothing.None;



        if (manager && manager.IsInitialized())
        {
            //backgroundImage.renderer.material.mainTexture = manager.GetUsersClrTex();
            if (backgroundImage && (backgroundImage.texture == null))
            {
                backgroundImage.texture = manager.GetUsersClrTex();
            }

            //			Vector3 vRight = BottomRight - BottomLeft;
            //			Vector3 vUp = TopLeft - BottomLeft;

            int iJointIndex = (int)TrackedJoint;
            int iJointIndexSholderRight = (int)TrackedJointShoulderRight;
            int iJointIndexSholderLeft = (int)TrackedJointShoulderLeft;

            if (manager.IsUserDetected())
            {
                /*if (throwListener)
                {
                    if (throwListener.IsSwipeLeft())
                    {
                        OverlayObject.transform.position = Vector3.Lerp(OverlayObject.transform.position, new Vector3(5, 1), smoothFactor * Time.deltaTime);
                        this.ballThrew = true;
                    }
                }*/

                uint userId = manager.GetPlayer1ID();


                this.userHandPos = manager.GetRawSkeletonJointPos(userId, iJointIndex);
                this.userShoulderRightPos = manager.GetRawSkeletonJointPos(userId, iJointIndexSholderRight);
                this.userShoulderLeftPos = manager.GetRawSkeletonJointPos(userId, iJointIndexSholderLeft);

                this.timestamp = Time.realtimeSinceStartup;



                if (!isThrow)
                {
                    this.textbox.info = "Wykonaj gest rzutu prawa reka.";
                    isThrow = FindThrow();
                }

                if (manager.IsJointTracked(userId, iJointIndex))
                {
                    Vector3 posJoint = manager.GetRawSkeletonJointPos(userId, iJointIndex);

                    if (posJoint != Vector3.zero)
                    {
                        // 3d position to depth
                        Vector2 posDepth = manager.GetDepthMapPosForJointPos(posJoint);

                        // depth pos to color pos

                        Vector2 posColor = manager.GetColorMapPosForDepthPos(posDepth);

                        float scaleX = (float)posColor.x / KinectWrapper.Constants.ColorImageWidth;
                        float scaleY = 1.0f - (float)posColor.y / KinectWrapper.Constants.ColorImageHeight;


                        //						Vector3 localPos = new Vector3(scaleX * 10f - 5f, 0f, scaleY * 10f - 5f); // 5f is 1/2 of 10f - size of the plane
                        //						Vector3 vPosOverlay = backgroundImage.transform.TransformPoint(localPos);
                        //Vector3 vPosOverlay = BottomLeft + ((vRight * scaleX) + (vUp * scaleY));

                        if (debugText)
                        {
                            debugText.GetComponent<GUIText>().text = "Tracked user ID: " + userId;  // new Vector2(scaleX, scaleY).ToString();
                        }

                        if (throwListener.IsRiseLeftHand())
                        {
                            this.ballThrew = false;
                            this.state = 0;
                            this.isThrow = false;

                            this.oldSpeed = 0;


                            this.textbox.info = "Wykonaj gest rzutu prawa reka.";

                        }



                        if (!ballThrew && throwListener)
                        {
                            //kiedy nie wykonano rzutu i widzi obiekt
                            if (OverlayObject)
                            {
                                //ustalanie pozycji kuli na ekranie na podstawie pozycji dloni
                                Vector3 vPosOverlay = Camera.main.ViewportToWorldPoint(new Vector3(scaleX, scaleY, (posJoint.z * (-1) + 3)));
                                //przesuniecie kuli pomiedzy pozycjami z t-1 a t
                                OverlayObject.transform.position = Vector3.Lerp(OverlayObject.transform.position, vPosOverlay, smoothFactor * Time.deltaTime);

                            }
                            //kiedy wykonano gest rzutu
                            if (isThrow)
                            {
                                //wlaczenie grawitacji - oderwanie pilki od reki
                                OverlayObject.GetComponent<Rigidbody>().useGravity = true;
                                this.acceleration = 2;
                                //nadanie pilce przedkosci w pojedynczej klatce
                                OverlayObject.GetComponent<Rigidbody>().AddForce(this.speedx * this.acceleration, this.speedy * this.acceleration, Mathf.Abs(this.speedz) * this.acceleration, ForceMode.VelocityChange);
                                //wartosci pogladowe
                                this.oldBallPos = OverlayObject.GetComponent<Rigidbody>().position;
                                this.oldTimestamp = this.timestamp;
                                this.ballThrew = true;
                            }
                        }
                        //jezeli wykonano gest rzutu
                        if (ballThrew && this.state != 4)
                        {
                            //wartosci pogladowe
                            float actualDis = Vector2.Distance(this.startBallPos, new Vector2(OverlayObject.GetComponent<Rigidbody>().position.x, OverlayObject.GetComponent<Rigidbody>().position.z));

                            this.textbox.info = "predkosc wyrzutu: " + (this.acceleration * oldSpeed).ToString("#0.0#;(#0.0#);-\0-") + "m/s \n" +
                                                "odleglosc rzutu: " + actualDis.ToString("#0.0#;(#0.0#);-\0-") + "m \n" +
                                                "kat wyrzutu: " + this.angle.ToString("#0.0#;(#0.0#);-\0-") + "\n";
                            if (OverlayObject.transform.position.y < 0.2)
                            {
                                //wylaczenie grawitacji i wyhamowanie pilki
                                OverlayObject.GetComponent<Rigidbody>().useGravity = false;
                                OverlayObject.transform.position = new Vector3(OverlayObject.GetComponent<Rigidbody>().position.x, (float)0.2, OverlayObject.GetComponent<Rigidbody>().position.z);
                                Rigidbody.velocity = Vector3.zero;
                                this.state = 4;
                                textbox.info += "Aby wykonac rzut ponownie, unies lewa reke";
                            }
                        }
                    }
                }
            }
            else if (this.state < 3)
            {
                this.textbox.info = "";
            }
        }
    }

    //skrypt wykrywania rzutu v2

    private bool FindThrow()
    {
        if (userHandPos.x != 0 && userHandPos.y != 0 && userHandPos.z != 0)
        {
            if (this.state == 0 && Mathf.Abs(userHandPos.y - userShoulderRightPos.y) < 0.2 && Vector3.Distance(userHandPos, userShoulderRightPos) < 0.3 && userShoulderRightPos.z > userShoulderLeftPos.z)
            {
                this.state = 1;
            }

            if (this.state == 1 && Mathf.Abs(userHandPos.y - userShoulderRightPos.y) < 0.3 && userHandPos.z < oldUserHandPos.z
               && userShoulderRightPos.z < oldUserShoulderRightPos.z && Vector3.Distance(userHandPos, oldUserHandPos) > 0.03)
            {
                //this.startUserHandPos = OverlayObject.GetComponent<Rigidbody>().position;
                this.startUserHandPos = this.userHandPos;
                this.startBallPos = new Vector2(OverlayObject.GetComponent<Rigidbody>().position.x, OverlayObject.GetComponent<Rigidbody>().position.z);
                this.oldUserHandPos = this.userHandPos;
                // this.oldUserShoulderLeftPos = this.userShoulderRightPos;
                // this.oldUserShoulderRightPos = this.userShoulderRightPos;
                this.oldTimestamp = this.timestamp;
                this.state = 2;
                return false;
            }
            else if (state == 1 && Vector3.Distance(userHandPos, userShoulderRightPos) > 0.3)
            {
                this.state = 0;
                return false;
            }


            if (this.state == 2)
            {
                this.distance = Vector3.Distance(oldUserHandPos, userHandPos);
                float ts = timestamp - this.oldTimestamp;
                this.speed = this.distance / ts;
                this.oldTimestamp = this.timestamp;
                //jezeli predkosc w poprzednim pomiarze jest nizsza program uznaje, ze rzut zostal wykonany
                //stan 3 -> rzut wykonany
                if (this.oldSpeed > this.speed)
                {
                    this.state = 3;
                    return true;
                }
                else  //wyliczanie pogladowych i wymaganych wartosci rzutu
                {
                    this.oldDistance = this.distance;
                    this.lastUserHandPos = this.userHandPos;
                    this.oldSpeed = this.speed;

                    float Dx = this.userHandPos.x - this.oldUserHandPos.x;
                    float Dy = this.userHandPos.y - this.oldUserHandPos.y;
                    float Dz = this.userHandPos.z - this.oldUserHandPos.z;

                    this.speedx = Dx / ts;
                    this.speedy = Dy / ts;
                    this.speedz = Dz / ts;

                    float sinB = Mathf.Sin(Dy / distance);
                    float kat = sinB * 180 / Mathf.PI;
                    this.oldUserHandPos = this.userHandPos;
                    this.angle = kat;
                    return false;
                }
            }
            this.oldUserHandPos = this.userHandPos;
            this.oldUserShoulderLeftPos = this.userShoulderRightPos;
            this.oldUserShoulderRightPos = this.userShoulderRightPos;
            this.oldTimestamp = this.timestamp;
            return false;
        }
        return false;
    }
}