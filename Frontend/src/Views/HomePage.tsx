import { defineComponent } from 'vue';
import Tile from '@/Components/UI/Tile/Tile';
import ActivityTile from '@/Components/UI/Tile/ActivityTile';
import type { Activity } from '@/Types/Activity';
import { Calendar, CircleCheckBig, Clock, TrendingUp, UsersRound } from 'lucide-vue-next';
import Button from '@/Components/UI/Button';

export default defineComponent({
  setup() {
    const activity1: Activity = { 
      id: 1,
      image: 'https://koala.svsticky.nl/rails/active_storage/representations/redirect/eyJfcmFpbHMiOnsibWVzc2FnZSI6IkJBaHBBbWNPIiwiZXhwIjpudWxsLCJwdXIiOiJibG9iX2lkIn19--4ee97e626759807734d88d6665b458613bf1f91d/eyJfcmFpbHMiOnsibWVzc2FnZSI6IkJBaDdCem9MWm05eWJXRjBTU0lJY0c1bkJqb0dSVlE2QzNKbGMybDZaVWtpRFRJMU5IZ3pOakFoQmpzR1ZBPT0iLCJleHAiOm51bGwsInB1ciI6InZhcmlhdGlvbiJ9fQ==--df7ad6130171d4ba961b6bb3a40f5477e5aac46a/Save%20the%20date%20poster.png',
      title: 'Study Trip',
      summary: '26 november is de hype-avond voor de studiereis!\n\nWe gaan weer met Sticky het verre buitenland opzoeken! Deze keer buiten de grenzen van Europa duiken we in een compleet nieuwe wereld vol cultuur, bijzondere ervaringen en geweldig eten.\n\nTijdens de avond onthullen we eindelijk de bestemming en kun jij kijken of je goed gegokt hebt. We geven een presentatie met alle belangrijke informatie die jij nodig hebt om mee te gaan. Na de presentatie opent de inschrijfperiode en heb je tot en met 12 december de tijd om je aan te melden en je motivatie in te sturen.\n\nWij van de studiereis commissie gaan er een mooie reis van maken en we hopen dat natuurlijk jij mee gaat!',
      price: 0,
      numberOfParticipants: 1,
      maxParticipants: 1,
      startdate: new Date('2024-06-01T10:00:00'),
      enddate: new Date('2024-06-01T10:05:00'),
      location: 'Vagant',
      committee: 'Studiereis'
     }; // TO DO: fetch from API
    
    const name = 'Rens'; // TO DO: fetch from user session
    
    return () => (
      <div>
        {<div class="flex flex-col align-items-center gap-5 max-w-8xl mx-auto">
          <Tile class='w-full m-0 bg-[#fa6b20] text-white'>
            <div class="flex gap-5">
              <div class="flex flex-col gap-5 flex-grow-1 basis-0">
                <p class="text-2xl font-semibold">Hey {name}!</p>
                <div class="flex gap-5">
                  <Tile class="bg-[#ff7225] border-[#gf8235 flex-grow-1">
                    <p>Aanmeldingen</p>
                    <div style="display: flex; align-items: center; gap: 8px;">
                      <p style="font-size: 32px;">3</p> <CircleCheckBig />
                    </div>
                  </Tile>
                  <Tile class="bg-[#ff7225] border-[#gf8235 flex-grow-1">
                    <p>Bijgewoond</p>
                    <div style="display: flex; align-items: center; gap: 8px;">
                      <p style="font-size: 32px;">12</p><TrendingUp />
                    </div>
                  </Tile>
                </div>
                <Tile class="bg-[#ff7225] border-[#gf8235 flex-grow-1">
                  <div class="flex justify-between items-center">
                    <div>
                      <p>Openstaand</p>
                      <p>€ 45,00</p>
                    </div>
                    <Button>Betalen</Button>
                  </div>
                </Tile>
              </div>
              <Tile class="flex flex-col gap-4 bg-[#ff7225] border-[#gf8235] flex-grow-1 basis-0">
                <div class="flex items-center gap-2"><Clock /> Eerstvolgende activiteit</div>
                <p>{activity1.title}</p>
                <div class="flex items-center gap-2"><Calendar /> {activity1.startdate.toLocaleDateString('default', {
                  day: 'numeric',
                  month: 'long',
                  year: 'numeric',
                  hour: '2-digit',
                  minute: '2-digit',
                  hour12: false,
                })}</div>
                <div class="flex items-center gap-2"><UsersRound /> {activity1.maxParticipants - activity1.numberOfParticipants} van de {activity1.maxParticipants} vrij </div>
                <Button showArrow={true}>Bekijk details</Button>
              </Tile>
            </div>
          </Tile>
          <div class="flex w-full justify-between items-center">
            <p class="font-semibold text-lg">Aankomende activiteiten:</p>
            <Button showArrow={true} class='bg-transparent p-0 hover:bg-transparent hover:text-[#FD8037]'>Bekijk alles</Button>
          </div>
          <div class="flex px-2 gap-5">
            <ActivityTile activity={activity1}/>
            <ActivityTile activity={activity1}/>
            <ActivityTile activity={activity1}/>
            <ActivityTile activity={activity1}/>
          </div>
          <div>
            <p>Mijn aanmeldingen</p>
          </div>
        </div>}
      </div>
    );
  },
});