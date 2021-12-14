import { photo } from "./photo";
export interface Member {
  id: number;
  username: string;
  photoUrl: string;
  age: string;
  knownAs: string;
  created: string;
  lastActive: string;
  gender: string;
  itroduction: string;
  lookingFor: string;
  interests: string;
  city: string;
  country: string;
  photos: photo[];
}
