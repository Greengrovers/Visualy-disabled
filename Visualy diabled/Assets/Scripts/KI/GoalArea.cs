using UnityEngine;
 using System.Collections.Generic; 
 public class GoalArea : MonoBehaviour
 { public float triggerRadius = 0.01f; 
 public LayerMask sheepLayer; private HashSet<SheepController> alreadyScored = new HashSet<SheepController>(); 
 private void Update() { Collider[] hits = Physics.OverlapSphere(transform.position, triggerRadius, sheepLayer); 
 foreach (Collider hit in hits) { SheepController sheep = hit.GetComponentInParent<SheepController>();
  if (sheep == null) continue; if (alreadyScored.Contains(sheep)) continue; alreadyScored.Add(sheep);
   if (ScoreManager.Instance != null) { ScoreManager.Instance.AddSheepScore(); } sheep.EnterGoal(); } }
    private void OnDrawGizmosSelected() { Gizmos.color = Color.green; 
    Gizmos.DrawWireSphere(transform.position, triggerRadius); }}