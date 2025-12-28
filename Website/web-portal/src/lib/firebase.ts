import { getApps, initializeApp } from "firebase/app";
import { getFirestore } from "firebase/firestore";
import { publicEnv } from "./env";

const firebaseConfig = {
  apiKey: publicEnv.firebase.apiKey,
  authDomain: publicEnv.firebase.authDomain,
  projectId: publicEnv.firebase.projectId,
  storageBucket: publicEnv.firebase.storageBucket,
  messagingSenderId: publicEnv.firebase.messagingSenderId,
  appId: publicEnv.firebase.appId,
};

const app = getApps().length === 0 ? initializeApp(firebaseConfig) : getApps()[0];

export const db = getFirestore(app);

