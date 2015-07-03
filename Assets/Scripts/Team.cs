using UnityEngine;
using System.Collections;

public class Team : MonoBehaviour {
	public int teamNumber;

	public bool IsOnMyTeam(Team team) {
		if (team == null) {
			return false;
		}
		return team.teamNumber == this.teamNumber;
	}
}
