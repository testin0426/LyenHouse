using UnityEngine;

namespace SunnyItems
{
    public class Trigger : MonoBehaviour
    {
        public GameObject[] food;
        public GameObject[] special_food;
        public GameObject[] sweets;
        public GameObject[] special_sweets;
        public float special_rate = 0.05f;
        public float extend_Distance = 0.4f;

        public int pickupNomber = 0;
        public bool special = false;
        public int fondueType = 0;

        [SerializeField] private Animator steam_animator;
        [SerializeField] private Transform extendBody;
        [SerializeField] private Transform extendHead;

        private Vector3 point;

        private GameObject[] foodchoco;
        private GameObject[] special_foodchoco;
        private GameObject[] sweetschoco;
        private GameObject[] special_sweetschoco;
        private GameObject[] foodcheese;
        private GameObject[] special_foodcheese;
        private GameObject[] sweetscheese;
        private GameObject[] special_sweetscheese;

        private float dist;

        private void Start()
        {
            foodcheese = new GameObject[food.Length];
            special_foodcheese = new GameObject[special_food.Length];
            foodchoco = new GameObject[food.Length];
            special_foodchoco = new GameObject[special_food.Length];
            sweetscheese = new GameObject[sweets.Length];
            special_sweetscheese = new GameObject[special_sweets.Length];
            sweetschoco = new GameObject[sweets.Length];
            special_sweetschoco = new GameObject[special_sweets.Length];

            for (int s = 0; s < food.Length; ++s)
            {
                foodchoco[s] = food[s].transform.Find("choco").gameObject;
                foodcheese[s] = food[s].transform.Find("cheese").gameObject;
            }
            for (int s = 0; s < special_food.Length; ++s)
            {
                special_foodchoco[s] = special_food[s].transform.Find("choco").gameObject;
                special_foodcheese[s] = special_food[s].transform.Find("cheese").gameObject;
            }
            for (int s = 0; s < sweets.Length; ++s)
            {
                sweetschoco[s] = sweets[s].transform.Find("choco").gameObject;
                sweetscheese[s] = sweets[s].transform.Find("cheese").gameObject;
            }
            for (int s = 0; s < special_sweets.Length; ++s)
            {
                special_sweetschoco[s] = special_sweets[s].transform.Find("choco").gameObject;
                special_sweetscheese[s] = special_sweets[s].transform.Find("cheese").gameObject;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.name == "Food_Trigger" && pickupNomber == 0)
            {
                float value = Random.value;
                if (value < special_rate)
                {
                    pickupNomber = Random.Range(1, special_food.Length + 1);
                    special = true;
                }
                else
                {
                    pickupNomber = Random.Range(1, food.Length + 1);
                }
            }
            else if (other.gameObject.name == "Sweets_Trigger" && pickupNomber == 0)
            {
                float value = Random.value;
                if (value < special_rate)
                {
                    pickupNomber = -Random.Range(1, special_sweets.Length + 1);
                    special = true;
                }
                else
                {
                    pickupNomber = -Random.Range(1, sweets.Length + 1);
                }
            }
            else if (other.gameObject.name == "Cheese_Trigger" && pickupNomber != 0 && fondueType == 0)
            {
                fondueType = 1;
            }
            else if (other.gameObject.name == "Choco_Trigger" && pickupNomber != 0 && fondueType == 0)
            {
                fondueType = 2;
            }

            Refresh();

            if (other.gameObject.name == "Cheese_Trigger" && fondueType == 1)
                Extend();
        }

        public void Refresh()
        {
            for (int s = 0; s < food.Length; ++s)
                food[s].SetActive(false);
            for (int s = 0; s < special_food.Length; ++s)
                special_food[s].SetActive(false);
            for (int s = 0; s < sweets.Length; ++s)
                sweets[s].SetActive(false);
            for (int s = 0; s < special_sweets.Length; ++s)
                special_sweets[s].SetActive(false);

            if (pickupNomber > 0)
            {
                if (special)
                    special_food[pickupNomber - 1].SetActive(true);
                else
                    food[pickupNomber - 1].SetActive(true);
            }
            else if (pickupNomber < 0)
            {
                if (special)
                    special_sweets[-pickupNomber - 1].SetActive(true);
                else
                    sweets[-pickupNomber - 1].SetActive(true);
            }

            if (fondueType == 0)
            {
                if (steam_animator != null)
                    steam_animator.SetBool("steam", false);

                for (int s = 0; s < foodchoco.Length; ++s) foodchoco[s].SetActive(false);
                for (int s = 0; s < special_foodchoco.Length; ++s) special_foodchoco[s].SetActive(false);
                for (int s = 0; s < sweetschoco.Length; ++s) sweetschoco[s].SetActive(false);
                for (int s = 0; s < special_sweetschoco.Length; ++s) special_sweetschoco[s].SetActive(false);
                for (int s = 0; s < foodcheese.Length; ++s) foodcheese[s].SetActive(false);
                for (int s = 0; s < special_foodcheese.Length; ++s) special_foodcheese[s].SetActive(false);
                for (int s = 0; s < sweetscheese.Length; ++s) sweetscheese[s].SetActive(false);
                for (int s = 0; s < special_sweetscheese.Length; ++s) special_sweetscheese[s].SetActive(false);
            }
            else if (fondueType == 1)
            {
                for (int s = 0; s < foodchoco.Length; ++s) foodchoco[s].SetActive(false);
                for (int s = 0; s < special_foodchoco.Length; ++s) special_foodchoco[s].SetActive(false);
                for (int s = 0; s < sweetschoco.Length; ++s) sweetschoco[s].SetActive(false);
                for (int s = 0; s < special_sweetschoco.Length; ++s) special_sweetschoco[s].SetActive(false);

                if (special && pickupNomber > 0)
                    special_foodcheese[pickupNomber - 1].SetActive(true);
                else if (special && pickupNomber < 0)
                    special_sweetscheese[-pickupNomber - 1].SetActive(true);
                else if (pickupNomber > 0)
                    foodcheese[pickupNomber - 1].SetActive(true);
                else if (pickupNomber < 0)
                    sweetscheese[-pickupNomber - 1].SetActive(true);

                if (steam_animator != null)
                    steam_animator.SetBool("steam", true);
            }
            else
            {
                for (int s = 0; s < foodcheese.Length; ++s) foodcheese[s].SetActive(false);
                for (int s = 0; s < special_foodcheese.Length; ++s) special_foodcheese[s].SetActive(false);
                for (int s = 0; s < sweetscheese.Length; ++s) sweetscheese[s].SetActive(false);
                for (int s = 0; s < special_sweetscheese.Length; ++s) special_sweetscheese[s].SetActive(false);

                if (special && pickupNomber > 0)
                    special_foodchoco[pickupNomber - 1].SetActive(true);
                else if (special && pickupNomber < 0)
                    special_sweetschoco[-pickupNomber - 1].SetActive(true);
                else if (pickupNomber > 0)
                    foodchoco[pickupNomber - 1].SetActive(true);
                else if (pickupNomber < 0)
                    sweetschoco[-pickupNomber - 1].SetActive(true);
            }
        }

        private void Update()
        {
            if (extendBody.gameObject.activeSelf)
            {
                extendHead.position = point;
                dist = Vector3.Distance(extendBody.position, extendHead.position);
                extendBody.LookAt(extendHead);
                extendHead.LookAt(extendBody);
                if (dist > extend_Distance)
                    Extend_disable();
            }
        }

        public void Extend()
        {
            extendBody.gameObject.SetActive(true);
            point = extendBody.position + new Vector3(0.0f, -0.02f, 0.0f);
        }

        public void Extend_disable()
        {
            extendBody.gameObject.SetActive(false);
        }

        public void Eat()
        {
            Extend_disable();
            pickupNomber = 0;
            special = false;
            fondueType = 0;
            Refresh();
        }

        public void ChangeOwner()
        {
            // No ownership in a single-player port.
        }
    }
}
