import { initializeApp } from 'firebase/app';
import { getAuth, RecaptchaVerifier, signInWithPhoneNumber, ConfirmationResult } from 'firebase/auth';

// Firebase configuration from Firebase Console
const firebaseConfig = {
  apiKey: "AIzaSyDf9lKwDgkaDNg2NncHWtSdKZS4F0vAtQM",
  authDomain: "kisansetu-501110.firebaseapp.com",
  projectId: "kisansetu-501110",
  storageBucket: "kisansetu-501110.firebasestorage.app",
  messagingSenderId: "362179805245",
  appId: "1:362179805245:web:03cf83379f3d7ad620ea54",
  measurementId: "G-ZJ8Q7FDD9M"
};

// Initialize Firebase
const app = initializeApp(firebaseConfig);
const auth = getAuth(app);

export { app, auth, RecaptchaVerifier, signInWithPhoneNumber };
export type { ConfirmationResult };
