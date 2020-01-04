using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;
using System.IO;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (CharacterController))]
    [RequireComponent(typeof (AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private bool m_IsWalking;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

        private Camera m_Camera;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private AudioSource m_AudioSource;
        private canvas q;
        private string position;
        public Text time;
        public AudioClip[] music;
        private AudioSource music_clip;

        GameObject children_gameObject;
        GameObject gameover_canvas;
        GameObject good_game_canvas;
        public canvas answer;
        // Use this for initialization
        private void Start()
        {
            AudioClip[] music = new AudioClip[1];
            music_clip = GetComponent<AudioSource>();
            answer = gameObject.transform.GetChild(0).gameObject.GetComponent<canvas>();
            time = gameObject.transform.GetChild(2).gameObject.transform.GetChild(0).gameObject.GetComponent<Text>();
            q = GameObject.FindObjectOfType<canvas>();
            position = "1";
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle/2f;
            m_Jumping = false;
            m_AudioSource = GetComponent<AudioSource>();
			m_MouseLook.Init(transform , m_Camera.transform);
            //transform.position = new Vector3(-6.76f, 1.48f, -4.51f);
        }


        // Update is called once per frame
        private void Update()
        {
            RotateView();
            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }

            m_PreviouslyGrounded = m_CharacterController.isGrounded;
        }


        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void FixedUpdate()
        {
            //count time
            time.text = (float.Parse(time.text) - Time.deltaTime * 1).ToString();
            if(float.Parse(time.text) < 0f)
            {
                m_MouseLook.lockCursor = false;
                Cursor.visible = true;
                gameover_canvas = gameObject.transform.GetChild(3).gameObject;
                gameover_canvas.gameObject.SetActive(true);
            }

            //right answer
            if (Input.GetKeyDown(KeyCode.Alpha1) && answer.select_right == 0)//answer and quit
            {
                music_clip.Stop();
                children_gameObject = gameObject.transform.GetChild(0).gameObject;
                children_gameObject.gameObject.SetActive(false);
                bstop = false;
                m_CharacterController.enabled = true;
            }
            if (Input.GetKeyDown(KeyCode.Alpha2) && answer.select_right == 1)//answer and quit
            {
                music_clip.Stop();
                children_gameObject = gameObject.transform.GetChild(0).gameObject;
                children_gameObject.gameObject.SetActive(false);
                bstop = false;
                m_CharacterController.enabled = true;
            }
            if (Input.GetKeyDown(KeyCode.Alpha3) && answer.select_right == 2)//answer and quit
            {
                music_clip.Stop();
                children_gameObject = gameObject.transform.GetChild(0).gameObject;
                children_gameObject.gameObject.SetActive(false);
                bstop = false;
                m_CharacterController.enabled = true;
            }
            if (Input.GetKeyDown(KeyCode.Alpha4) && answer.select_right == 3)//answer and quit
            {
                music_clip.Stop();
                children_gameObject = gameObject.transform.GetChild(0).gameObject;
                children_gameObject.gameObject.SetActive(false);
                bstop = false;
                m_CharacterController.enabled = true;
            }//wrong asnwer
            else if (bstop && (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Alpha2)))
            {
                time.text = (float.Parse(time.text) - 10).ToString();
            }

            float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height/2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            m_MoveDir.x = desiredMove.x*speed;
            m_MoveDir.z = desiredMove.z*speed;


            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                }
            }
            else
            {
                m_MoveDir += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
            }
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);

            m_MouseLook.UpdateCursorLock();
        }


        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }


        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            if (bstop == false)
            {
                int n = Random.Range(1, m_FootstepSounds.Length);
                m_AudioSource.clip = m_FootstepSounds[n];
                m_AudioSource.PlayOneShot(m_AudioSource.clip);
                // move picked sound to index 0 so it's not picked next time
                m_FootstepSounds[n] = m_FootstepSounds[0];
                m_FootstepSounds[0] = m_AudioSource.clip;
            }
        }


        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }


        private void GetInput(out float speed)
        {
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
            m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif
            // set the desired speed to be walking or running
            speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
            m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }
        }


        private void RotateView()
        {
            m_MouseLook.LookRotation (transform, m_Camera.transform);
        }

        Vector3 vstop;
        bool bstop = false;
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            
            //random question hit
            if(hit.transform.tag == "random_question" && bstop == false)
            {
                music_clip.clip = music[0];
                music_clip.Play();
                children_gameObject = gameObject.transform.GetChild(0).gameObject;
                children_gameObject.gameObject.SetActive(true);
                q = GameObject.FindObjectOfType<canvas>();
                q.startquestion();
                hit.transform.tag = "Untagged";
                vstop = transform.position;
                bstop = true; // stop and anser the question
                Destroy(hit.transform.gameObject);
            }
            //condition question hit
            if((hit.transform.tag == "condition_stair" || hit.transform.tag == "condition_bathroom") && bstop == false)
            {
                children_gameObject = gameObject.transform.GetChild(0).gameObject;
                children_gameObject.gameObject.SetActive(true);
                q = GameObject.FindObjectOfType<canvas>();
                q.condition_quesiton(hit.transform.tag);
                hit.transform.tag = "Untagged";
                vstop = transform.position;
                bstop = true; // stop and anser the question
                Destroy(hit.transform.gameObject);
            }
            //test question hit
            if(hit.transform.name == "stair1_1")
            {
                
                children_gameObject = gameObject.transform.GetChild(0).gameObject;
                children_gameObject.gameObject.SetActive(true);
                q = GameObject.FindObjectOfType<canvas>();
                q.startquestion();
                hit.transform.name = "finish_question";
                vstop = transform.position;
                bstop = true; // stop and anser the question
            }
            //dont move
            if (bstop)
            {
                m_CharacterController.enabled = false;
                //transform.position = vstop;
                //m_CharacterController.enabled = true;
            }

            //door
            if (hit.transform.name == "door1-1")
            {
                m_CharacterController.enabled = false;
                transform.position = new Vector3(-15.22092f, 1.48f, -4.338239f);
                m_CharacterController.enabled = true;
            }
            if (hit.transform.name == "door1-2")
            {
                m_CharacterController.enabled = false;
                transform.position = new Vector3(-10.93121f, 1.48f, -4.338239f);
                m_CharacterController.enabled = true;
            }
            if (hit.transform.name == "door2-1")
            {
                m_CharacterController.enabled = false;
                transform.position = new Vector3(-15.22092f, 11.38792f, -4.338239f);
                m_CharacterController.enabled = true;
            }
            if (hit.transform.name == "door2-2")
            {
                m_CharacterController.enabled = false;
                transform.position = new Vector3(-10.93121f, 11.38792f, -4.338239f);
                m_CharacterController.enabled = true;
            }
            if (hit.transform.name == "door2-3")
            {
                m_CharacterController.enabled = false;
                transform.position = new Vector3(14f, 11.38792f, -4.338239f);
                m_CharacterController.enabled = true;
            }
            if (hit.transform.name == "door2-4")
            {
                m_CharacterController.enabled = false;
                transform.position = new Vector3(11.12511f, 11.38792f, -4.338239f);
                m_CharacterController.enabled = true;
            }
            if (hit.transform.name == "door2-5")
            {
                m_CharacterController.enabled = false;
                transform.position = new Vector3(9.06193f, 11.38792f, 1.490116e-08f);
                m_CharacterController.enabled = true;
            }
            if (hit.transform.name == "door2-6")
            {
                m_CharacterController.enabled = false;
                transform.position = new Vector3(9.06193f, 11.38792f, -3.533898f);
                m_CharacterController.enabled = true;
            }
            if (hit.transform.name == "door3-1")
            {
                m_CharacterController.enabled = false;
                transform.position = new Vector3(-15.22092f, 20.48604f, -4.338239f);
                m_CharacterController.enabled = true;
            }
            if (hit.transform.name == "door3-2")
            {
                m_CharacterController.enabled = false;
                transform.position = new Vector3(-10.93121f, 20.48604f, -4.338239f);
                m_CharacterController.enabled = true;
            }
            if (hit.transform.name == "door3-3")
            {
                m_CharacterController.enabled = false;
                transform.position = new Vector3(10f, 20.48604f, -4.338239f);
                m_CharacterController.enabled = true;
            }
            if (hit.transform.name == "door3-4")
            {
                m_CharacterController.enabled = false;
                transform.position = new Vector3(4.875444f, 20.48604f, -4.338239f);
                m_CharacterController.enabled = true;
            }
            if (hit.transform.name == "windowevent" && float.Parse(time.text) <= 30f)
            {
                m_MouseLook.lockCursor = false;
                Cursor.visible = true;
                good_game_canvas = gameObject.transform.GetChild(4).gameObject;
                good_game_canvas.gameObject.SetActive(true);
            }

            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
        }
    }
}
