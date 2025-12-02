export type Activity = {
	id: number;
	image: string;
	title: string;
	summary: string;
	price: number;
	numberOfParticipants: number;
	maxParticipants: number;
	startdate: Date;
	enddate: Date;
	location: string;
	committee: string;
	question?: string;
	answer?: string;
};
